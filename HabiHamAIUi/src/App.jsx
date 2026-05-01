import { useMemo, useState } from "react";
import { BrowserRouter, Link, Navigate, Route, Routes, useNavigate } from "react-router-dom";
import TopNav from "./TopNav";

function AppContent() {
  const navigate = useNavigate();
  const [tab, setTab] = useState("ai");
  const [baseUrl, setBaseUrl] = useState("http://localhost:5193");
  const [accessToken, setAccessToken] = useState("");
  const [adminToken, setAdminToken] = useState("");
  const [aiToken, setAiToken] = useState("");
  const [currentUserName, setCurrentUserName] = useState("");
  const [currentUserRole, setCurrentUserRole] = useState("");
  const [errorView, setErrorView] = useState("No errors.");

  const [registerUsername, setRegisterUsername] = useState("user1");
  const [registerPassword, setRegisterPassword] = useState("user1234");
  const [username, setUsername] = useState("admin");
  const [password, setPassword] = useState("admin123");

  const [dialogs, setDialogs] = useState([]);
  const [currentDialogId, setCurrentDialogId] = useState("");
  const [chatPrompt, setChatPrompt] = useState("");
  const [chatMessages, setChatMessages] = useState([
    { role: "assistant", content: "Привет! Войди через Login и отправь сообщение." }
  ]);
  const [workoutSessions, setWorkoutSessions] = useState([]);
  const [workoutsSubTab, setWorkoutsSubTab] = useState("programs");
  const [workoutExerciseCatalog, setWorkoutExerciseCatalog] = useState([]);
  const [selectedCatalogExerciseId, setSelectedCatalogExerciseId] = useState("");
  const [newCatalogExerciseName, setNewCatalogExerciseName] = useState("");
  const [newCatalogExerciseMeta, setNewCatalogExerciseMeta] = useState("");
  const [isCreateExerciseModalOpen, setIsCreateExerciseModalOpen] = useState(false);
  const [programCode, setProgramCode] = useState("");
  const [programDay, setProgramDay] = useState("");
  const [programNotes, setProgramNotes] = useState("");
  const [programExercisesDraft, setProgramExercisesDraft] = useState([]);
  const [isProgramModalOpen, setIsProgramModalOpen] = useState(false);
  const [isProgramDeleteModalOpen, setIsProgramDeleteModalOpen] = useState(false);
  const [editingProgramId, setEditingProgramId] = useState("");
  const [pendingDeleteProgram, setPendingDeleteProgram] = useState(null);
  const [selectedProgramCode, setSelectedProgramCode] = useState("");
  const [currentWorkout, setCurrentWorkout] = useState(null);
  const [profileBirthDate, setProfileBirthDate] = useState("");
  const [profileHeightCm, setProfileHeightCm] = useState("");
  const [profileWeightKg, setProfileWeightKg] = useState("");
  const [profilePhone, setProfilePhone] = useState("");
  const [profileCity, setProfileCity] = useState("");
  const [profileAbout, setProfileAbout] = useState("");

  const [users, setUsers] = useState([]);
  const [adminCreateUsername, setAdminCreateUsername] = useState("");
  const [adminCreatePassword, setAdminCreatePassword] = useState("");
  const [adminCreateRole, setAdminCreateRole] = useState("AiUser");
  const [isCreateUserModalOpen, setIsCreateUserModalOpen] = useState(false);
  const [isEditUserModalOpen, setIsEditUserModalOpen] = useState(false);
  const [isPasswordModalOpen, setIsPasswordModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [selectedAdminUser, setSelectedAdminUser] = useState(null);
  const [adminManageUserId, setAdminManageUserId] = useState("");
  const [editUserName, setEditUserName] = useState("");
  const [editUserRole, setEditUserRole] = useState("User");
  const [newPasswordValue, setNewPasswordValue] = useState("");
  const [adminDialogUserId, setAdminDialogUserId] = useState("");
  const [adminDialogs, setAdminDialogs] = useState([]);
  const [adminCurrentDialogId, setAdminCurrentDialogId] = useState("");
  const [adminDialogMessages, setAdminDialogMessages] = useState([]);

  async function request(method, path, body, token) {
    try {
      const response = await fetch(baseUrl.trim().replace(/\/+$/, "") + path, {
        method,
        headers: {
          ...(body ? { "Content-Type": "application/json" } : {}),
          ...(token ? { Authorization: "Bearer " + token } : {})
        },
        ...(body ? { body: JSON.stringify(body) } : {})
      });
      const data = await response.json().catch(() => ({}));
      return { status: response.status, ok: response.ok, data };
    } catch (error) {
      return { status: 0, ok: false, data: { message: "Network error", detail: String(error) } };
    }
  }

  function handleResult(result) {
    setErrorView(result.ok ? "No errors." : JSON.stringify({
      status: result.status,
      message: result.data?.message ?? "Request failed",
      detail: result.data
    }, null, 2));
  }

  function tryGetUserNameFromToken(token) {
    try {
      const payloadPart = token.split(".")[1];
      if (!payloadPart) return "";
      const normalized = payloadPart.replace(/-/g, "+").replace(/_/g, "/");
      const padded = normalized + "=".repeat((4 - (normalized.length % 4)) % 4);
      const payload = JSON.parse(window.atob(padded));
      return payload.unique_name || payload.name || payload.sub || "";
    } catch {
      return "";
    }
  }

  function tryGetRoleFromToken(token) {
    try {
      const payloadPart = token.split(".")[1];
      if (!payloadPart) return "";
      const normalized = payloadPart.replace(/-/g, "+").replace(/_/g, "/");
      const padded = normalized + "=".repeat((4 - (normalized.length % 4)) % 4);
      const payload = JSON.parse(window.atob(padded));
      const rawRole = payload.role || payload.roles || payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || "";
      if (Array.isArray(rawRole)) return rawRole[0] || "";
      return String(rawRole || "");
    } catch {
      return "";
    }
  }

  async function loadDialogs(token = aiToken, forceDialogId = "") {
    if (!token) return;
    const result = await request("GET", "/ai/dialogs", null, token);
    handleResult(result);
    if (!result.ok) return;
    const incoming = Array.isArray(result.data) ? result.data : [];
    setDialogs(incoming);
    const nextDialogId = forceDialogId || (incoming.some((d) => d.id === currentDialogId) ? currentDialogId : incoming[0]?.id || "");
    setCurrentDialogId(nextDialogId);
    if (nextDialogId) {
      await loadDialogMessages(nextDialogId, token);
    } else {
      setChatMessages([{ role: "assistant", content: "Создай диалог, чтобы начать чат." }]);
    }
  }

  async function loadDialogMessages(dialogId, token = aiToken) {
    if (!dialogId) return;
    const result = await request("GET", `/ai/dialogs/${dialogId}/messages`, null, token);
    handleResult(result);
    if (!result.ok) return;
    const messages = Array.isArray(result.data) ? result.data : [];
    setChatMessages(messages.length ? messages : [{ role: "assistant", content: "Пустой диалог. Отправь первое сообщение." }]);
  }

  async function loginAndStore(navigate) {
    const result = await request("POST", "/auth/login", { username: username.trim(), password });
    handleResult(result);
    if (!result.ok) return;
    const token = result.data?.accessToken || "";
    setAccessToken(token);
    setAdminToken(token);
    setAiToken(token);
    const nextRole = tryGetRoleFromToken(token);
    setCurrentUserName(tryGetUserNameFromToken(token) || username.trim());
    setCurrentUserRole(nextRole);
    setTab(nextRole === "Admin" || nextRole === "AiUser" ? "ai" : "profile");
    if (nextRole === "Admin" || nextRole === "AiUser") {
      await loadDialogs(token);
    }
    await loadMyProfile(token);
    navigate("/app");
  }

  async function sendChat() {
    const prompt = chatPrompt.trim();
    if (!prompt) return setErrorView("Enter a message for AI chat.");
    if (!aiToken) return setErrorView("Login first or paste JWT token in AI Token field.");

    setChatMessages((prev) => [...prev, { role: "user", content: prompt }]);
    setChatPrompt("");

    const result = await request("POST", "/ai/chat", { prompt, dialogId: currentDialogId || null }, aiToken);
    handleResult(result);
    if (!result.ok) {
      setChatMessages((prev) => [...prev, { role: "assistant", content: "AI request failed." }]);
      return;
    }

    const dialogId = result.data?.dialogId || currentDialogId;
    setCurrentDialogId(dialogId);
    setChatMessages((prev) => [...prev, { role: "assistant", content: result.data?.response || "No response text." }]);
    await loadDialogs(aiToken, dialogId);
  }

  async function loadUsers() {
    const result = await request("GET", "/admin/users", null, adminToken);
    handleResult(result);
    if (!result.ok) return;
    const incomingUsers = Array.isArray(result.data) ? result.data : [];
    setUsers(incomingUsers);
    if (!adminManageUserId && incomingUsers[0]?.id) {
      setAdminManageUserId(incomingUsers[0].id);
    }
    if (!adminDialogUserId && incomingUsers[0]?.id) {
      setAdminDialogUserId(incomingUsers[0].id);
    }
  }

  async function loadMyProfile(token = accessToken) {
    if (!token) return;
    const result = await request("GET", "/users/me", null, token);
    handleResult(result);
    if (!result.ok) return;

    const profile = result.data || {};
    setProfileBirthDate(profile.birthDate || "");
    setProfileHeightCm(profile.heightCm === null || profile.heightCm === undefined ? "" : String(profile.heightCm));
    setProfileWeightKg(profile.weightKg === null || profile.weightKg === undefined ? "" : String(profile.weightKg));
    setProfilePhone(profile.phone || "");
    setProfileCity(profile.city || "");
    setProfileAbout(profile.about || "");
  }

  async function loadWorkoutSessions(token = accessToken) {
    if (!token) return;
    const result = await request("GET", "/users/me/workouts", null, token);
    handleResult(result);
    if (!result.ok) return;
    setWorkoutSessions(Array.isArray(result.data) ? result.data : []);
  }

  async function loadWorkoutExerciseCatalog(token = accessToken) {
    if (!token) return;
    const result = await request("GET", "/users/me/workouts/exercises", null, token);
    handleResult(result);
    if (!result.ok) return;

    const incoming = Array.isArray(result.data) ? result.data : [];
    const unique = [];
    const seen = new Set();
    for (const item of incoming) {
      const key = (item.name || "").trim().toLowerCase();
      if (!key || seen.has(key)) continue;
      seen.add(key);
      unique.push(item);
    }
    setWorkoutExerciseCatalog(unique);
    if (!selectedCatalogExerciseId && unique[0]?.id) {
      setSelectedCatalogExerciseId(unique[0].id);
    }
  }

  async function createCatalogExercise() {
    if (!accessToken) return;
    const name = (newCatalogExerciseName || "").trim();
    if (!name) return setErrorView("Укажи название упражнения.");

    const seedCode = `catalog::${slugifyProgramCode(name)}-${Date.now()}`;
    const result = await request(
      "POST",
      "/users/me/workouts",
      {
        sessionCode: seedCode,
        date: new Date().toISOString().slice(0, 10),
        day: `Каталог: ${name}`,
        notes: "Служебная запись для каталога упражнений",
        exercises: [
          {
            name,
            meta: (newCatalogExerciseMeta || "").trim(),
            sets: []
          }
        ]
      },
      accessToken
    );
    handleResult(result);
    if (!result.ok) return;

    setNewCatalogExerciseName("");
    setNewCatalogExerciseMeta("");
    setIsCreateExerciseModalOpen(false);
    await loadWorkoutExerciseCatalog(accessToken);
  }

  async function deleteCatalogExercise(exerciseId) {
    if (!accessToken || !exerciseId) return;
    if (!window.confirm("Удалить упражнение из каталога?")) return;

    const result = await request("DELETE", `/users/me/workouts/exercises/${exerciseId}`, null, accessToken);
    handleResult(result);
    if (!result.ok) return;

    if (String(selectedCatalogExerciseId) === String(exerciseId)) {
      setSelectedCatalogExerciseId("");
    }
    await loadWorkoutExerciseCatalog(accessToken);
  }

  function slugifyProgramCode(value) {
    const normalized = value.trim().toLowerCase().replace(/\s+/g, "-").replace(/[^a-z0-9а-яё_-]/gi, "");
    return normalized || `program-${Date.now()}`;
  }

  function parseProgramExerciseMeta(rawMeta) {
    const raw = String(rawMeta || "").trim();
    if (!raw) {
      return { isStructured: false, sourceExerciseId: "", comment: "", legacy: "" };
    }

    try {
      const parsed = JSON.parse(raw);
      if (parsed && typeof parsed === "object" && ("sourceExerciseId" in parsed || "comment" in parsed)) {
        return {
          isStructured: true,
          sourceExerciseId: parsed?.sourceExerciseId ? String(parsed.sourceExerciseId) : "",
          comment: parsed?.comment ? String(parsed.comment) : "",
          legacy: ""
        };
      }
    } catch {
      // keep legacy plain text meta
    }

    return { isStructured: false, sourceExerciseId: "", comment: "", legacy: raw };
  }

  function addExerciseToProgram(exercise) {
    setProgramExercisesDraft((prev) => [
      ...prev,
      {
        id: `pex-${Date.now()}-${Math.random()}`,
        sourceExerciseId: exercise.id || "",
        name: exercise.name || "",
        comment: ""
      }
    ]);
  }

  function removeProgramExercise(exerciseId) {
    setProgramExercisesDraft((prev) => prev.filter((x) => x.id !== exerciseId));
  }

  function updateProgramExerciseComment(exerciseId, comment) {
    setProgramExercisesDraft((prev) =>
      prev.map((x) =>
        x.id === exerciseId
          ? { ...x, comment }
          : x
      )
    );
  }

  async function saveProgramToDb() {
    if (!accessToken) return;
    const normalizedDay = programDay.trim();
    if (!normalizedDay) return setErrorView("Укажи название программы.");

    const code = `program::${slugifyProgramCode(programCode || normalizedDay)}`;
    const result = await request(
      "POST",
      "/users/me/workouts",
      {
        sessionCode: code,
        date: new Date().toISOString().slice(0, 10),
        day: normalizedDay,
        notes: programNotes.trim(),
        exercises: programExercisesDraft.map((x) => ({
          name: (x.name || "").trim(),
          meta: JSON.stringify({
            sourceExerciseId: x.sourceExerciseId || null,
            comment: (x.comment || "").trim()
          }),
          sets: []
        }))
      },
      accessToken
    );
    handleResult(result);
    if (!result.ok) return;
    await loadWorkoutSessions(accessToken);
    setEditingProgramId("");
    setProgramCode("");
    setProgramDay("");
    setProgramNotes("");
    setProgramExercisesDraft([]);
    setIsProgramModalOpen(false);
  }

  function openProgramCreateModal() {
    setEditingProgramId("");
    setProgramCode("");
    setProgramDay("");
    setProgramNotes("");
    setProgramExercisesDraft([]);
    setIsProgramModalOpen(true);
  }

  function openProgramEditModal(program) {
    setEditingProgramId(program.id || "");
    setProgramCode((program.sessionCode || "").replace(/^program::/, ""));
    setProgramDay(program.day || "");
    setProgramNotes(program.notes || "");
    setProgramExercisesDraft(
      (program.exercises || []).map((x, idx) => ({
        ...parseProgramExerciseMeta(x.meta),
        id: `edit-${idx}-${Date.now()}`,
        name: x.name || ""
      }))
    );
    setIsProgramModalOpen(true);
  }

  function closeProgramModal() {
    setIsProgramModalOpen(false);
    setEditingProgramId("");
  }

  function openProgramDeleteModal(program) {
    setPendingDeleteProgram(program);
    setIsProgramDeleteModalOpen(true);
  }

  async function deleteProgramFromModal() {
    if (!pendingDeleteProgram?.id || !accessToken) return;
    const result = await request("DELETE", `/users/me/workouts/${pendingDeleteProgram.id}`, null, accessToken);
    handleResult(result);
    if (!result.ok) return;

    if (selectedProgramCode && pendingDeleteProgram.sessionCode === selectedProgramCode) {
      setSelectedProgramCode("");
    }
    setPendingDeleteProgram(null);
    setIsProgramDeleteModalOpen(false);
    await loadWorkoutSessions(accessToken);
  }

  function startWorkoutFromProgram(program) {
    setCurrentWorkout({
      sessionCode: `workout::${Date.now()}`,
      day: program.day || "Тренировка",
      notes: "",
      exercises: (program.exercises || []).map((x, idx) => {
        const parsedMeta = parseProgramExerciseMeta(x.meta);
        return {
          id: `cw-${idx}-${Date.now()}`,
          name: x.name || "",
          meta: parsedMeta.comment || parsedMeta.legacy,
          sets: [{ weight: "", reps: "", rpe: "" }]
        };
      })
    });
  }

  function createWorkoutFromScratch() {
    setCurrentWorkout({
      sessionCode: `workout::${Date.now()}`,
      day: "Новая тренировка",
      notes: "",
      exercises: []
    });
  }

  function updateCurrentWorkoutField(field, value) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return { ...prev, [field]: value };
    });
  }

  function addExerciseToCurrentWorkout() {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: [
          ...prev.exercises,
          {
            id: `cw-manual-${Date.now()}-${Math.random()}`,
            name: "",
            meta: "",
            sets: [{ weight: "", reps: "", rpe: "" }]
          }
        ]
      };
    });
  }

  function updateCurrentWorkoutExercise(exerciseId, field, value) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.map((x) => (x.id === exerciseId ? { ...x, [field]: value } : x))
      };
    });
  }

  function removeCurrentWorkoutExercise(exerciseId) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.filter((x) => x.id !== exerciseId)
      };
    });
  }

  function updateCurrentWorkoutSet(exerciseId, setIndex, field, value) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.map((x) =>
          x.id === exerciseId
            ? { ...x, sets: x.sets.map((s, idx) => (idx === setIndex ? { ...s, [field]: value } : s)) }
            : x
        )
      };
    });
  }

  function addCurrentWorkoutSet(exerciseId) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.map((x) =>
          x.id === exerciseId
            ? { ...x, sets: [...x.sets, { weight: "", reps: "", rpe: "" }] }
            : x
        )
      };
    });
  }

  function removeCurrentWorkoutSet(exerciseId, setIndex) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.map((x) =>
          x.id === exerciseId
            ? { ...x, sets: x.sets.filter((_, idx) => idx !== setIndex) }
            : x
        )
      };
    });
  }

  async function saveCurrentWorkout() {
    if (!accessToken || !currentWorkout) return;
    if (!(currentWorkout.day || "").trim()) return setErrorView("Укажи название тренировки.");
    const result = await request(
      "POST",
      "/users/me/workouts",
      {
        sessionCode: currentWorkout.sessionCode,
        date: new Date().toISOString().slice(0, 10),
        day: currentWorkout.day.trim(),
        notes: currentWorkout.notes || "",
        exercises: currentWorkout.exercises.map((x) => ({
          name: (x.name || "").trim(),
          meta: x.meta,
          sets: x.sets.map((s) => ({ weight: s.weight, reps: s.reps, rpe: s.rpe }))
        })).filter((x) => x.name)
      },
      accessToken
    );
    handleResult(result);
    if (!result.ok) return;
    setCurrentWorkout(null);
    await loadWorkoutSessions(accessToken);
  }

  async function deleteWorkoutSession(sessionId) {
    if (!accessToken) return;
    if (!sessionId) return;
    if (!window.confirm("Удалить выбранную тренировку?")) return;

    const result = await request("DELETE", `/users/me/workouts/${sessionId}`, null, accessToken);
    handleResult(result);
    if (!result.ok) return;

    if (selectedProgramCode && workoutPrograms.some((x) => x.id === sessionId && x.sessionCode === selectedProgramCode)) {
      setSelectedProgramCode("");
    }
    await loadWorkoutSessions(accessToken);
  }

  async function saveMyProfile() {
    if (!accessToken) return;

    const toNumberOrNull = (value) => {
      const raw = value.trim();
      if (!raw) return null;
      const parsed = Number(raw);
      return Number.isNaN(parsed) ? null : parsed;
    };

    const body = {
      birthDate: profileBirthDate || null,
      heightCm: toNumberOrNull(profileHeightCm),
      weightKg: toNumberOrNull(profileWeightKg),
      phone: profilePhone.trim() || null,
      city: profileCity.trim() || null,
      about: profileAbout.trim() || null
    };

    const result = await request("PUT", "/users/me", body, accessToken);
    handleResult(result);
    if (result.ok) {
      await loadMyProfile(accessToken);
    }
  }

  async function loadAdminDialogs(forceUserId = adminDialogUserId, forceDialogId = "") {
    if (!adminToken) return;
    const query = forceUserId ? `?userId=${encodeURIComponent(forceUserId)}` : "";
    const result = await request("GET", `/admin/dialogs${query}`, null, adminToken);
    handleResult(result);
    if (!result.ok) return;
    const incoming = Array.isArray(result.data) ? result.data : [];
    setAdminDialogs(incoming);
    const nextDialogId = forceDialogId || (incoming.some((d) => d.id === adminCurrentDialogId) ? adminCurrentDialogId : incoming[0]?.id || "");
    setAdminCurrentDialogId(nextDialogId);
    if (nextDialogId) {
      await loadAdminDialogMessages(nextDialogId);
    } else {
      setAdminDialogMessages([]);
    }
  }

  async function loadAdminDialogMessages(dialogId = adminCurrentDialogId) {
    if (!dialogId) {
      setAdminDialogMessages([]);
      return;
    }
    const result = await request("GET", `/admin/dialogs/${dialogId}/messages`, null, adminToken);
    handleResult(result);
    if (!result.ok) return;
    setAdminDialogMessages(Array.isArray(result.data) ? result.data : []);
  }

  async function createAdminDialog() {
    if (!adminDialogUserId) return setErrorView("Выбери пользователя для нового диалога.");
    const title = window.prompt("Название нового диалога:", "Новый диалог");
    if (title === null) return;
    const result = await request("POST", "/admin/dialogs", { userId: adminDialogUserId, title }, adminToken);
    handleResult(result);
    if (result.ok) {
      await loadAdminDialogs(adminDialogUserId, result.data?.id);
    }
  }

  async function renameAdminDialog() {
    if (!adminCurrentDialogId) return setErrorView("Сначала выбери диалог.");
    const current = adminDialogs.find((d) => d.id === adminCurrentDialogId);
    const title = window.prompt("Новое название диалога:", current?.title || "Новый диалог");
    if (!title || !title.trim()) return;
    const result = await request("PUT", `/admin/dialogs/${adminCurrentDialogId}`, { title }, adminToken);
    handleResult(result);
    if (result.ok) {
      await loadAdminDialogs(adminDialogUserId, adminCurrentDialogId);
    }
  }

  async function deleteAdminDialog() {
    if (!adminCurrentDialogId) return setErrorView("Сначала выбери диалог.");
    if (!window.confirm("Удалить выбранный диалог?")) return;
    const deletingId = adminCurrentDialogId;
    const result = await request("DELETE", `/admin/dialogs/${deletingId}`, null, adminToken);
    handleResult(result);
    if (result.ok) {
      await loadAdminDialogs(adminDialogUserId);
    }
  }

  async function createAdminUser() {
    const result = await request(
      "POST",
      "/admin/users",
      { username: adminCreateUsername.trim(), password: adminCreatePassword, role: adminCreateRole },
      adminToken
    );
    handleResult(result);
    if (result.ok) {
      setAdminCreateUsername("");
      setAdminCreatePassword("");
      setAdminCreateRole("AiUser");
      setIsCreateUserModalOpen(false);
      await loadUsers();
    }
  }

  async function saveAdminUserFromModal() {
    if (!selectedAdminUser) return;
    const result = await request(
      "PUT",
      `/admin/users/${selectedAdminUser.id}`,
      { username: editUserName.trim(), role: editUserRole },
      adminToken
    );
    handleResult(result);
    if (result.ok) {
      setIsEditUserModalOpen(false);
      setSelectedAdminUser(null);
      await loadUsers();
    }
  }

  async function saveAdminPasswordFromModal() {
    if (!selectedAdminUser) return;
    if (!newPasswordValue.trim()) return setErrorView("Введи новый пароль.");
    const result = await request(
      "PUT",
      `/admin/users/${selectedAdminUser.id}/password`,
      { password: newPasswordValue },
      adminToken
    );
    handleResult(result);
    if (result.ok) {
      setNewPasswordValue("");
      setIsPasswordModalOpen(false);
      setSelectedAdminUser(null);
    }
  }

  async function deleteAdminUserFromModal() {
    if (!selectedAdminUser) return;
    const result = await request("DELETE", `/admin/users/${selectedAdminUser.id}`, null, adminToken);
    handleResult(result);
    if (result.ok) {
      setIsDeleteModalOpen(false);
      setSelectedAdminUser(null);
      await loadUsers();
    }
  }

  function openEditModal(user) {
    setSelectedAdminUser(user);
    setEditUserName(user.username || "");
    setEditUserRole(user.role || "User");
    setIsEditUserModalOpen(true);
  }

  function openPasswordModal(user) {
    setSelectedAdminUser(user);
    setNewPasswordValue("");
    setIsPasswordModalOpen(true);
  }

  function openDeleteModal(user) {
    setSelectedAdminUser(user);
    setIsDeleteModalOpen(true);
  }

  const dialogOptions = useMemo(
    () => dialogs.map((d) => <option key={d.id} value={d.id}>{d.title || "Dialog"}</option>),
    [dialogs]
  );
  const adminUserOptions = useMemo(
    () => users.map((u) => <option key={u.id} value={u.id}>{u.username} ({u.role})</option>),
    [users]
  );
  const adminDialogOptions = useMemo(
    () => adminDialogs.map((d) => <option key={d.id} value={d.id}>{d.title || "Dialog"} ({d.username || "unknown"})</option>),
    [adminDialogs]
  );
  const adminManageSelectedUser = useMemo(
    () => users.find((u) => u.id === adminManageUserId) || null,
    [users, adminManageUserId]
  );
  const workoutPrograms = useMemo(
    () => workoutSessions.filter((x) => String(x.sessionCode || "").startsWith("program::")),
    [workoutSessions]
  );
  const workoutLogs = useMemo(
    () => workoutSessions.filter((x) => String(x.sessionCode || "").startsWith("workout::")),
    [workoutSessions]
  );
  const selectedProgram = useMemo(
    () => workoutPrograms.find((x) => x.sessionCode === selectedProgramCode) || null,
    [workoutPrograms, selectedProgramCode]
  );
  const selectedCatalogExercise = useMemo(
    () => workoutExerciseCatalog.find((x) => String(x.id) === String(selectedCatalogExerciseId)) || null,
    [workoutExerciseCatalog, selectedCatalogExerciseId]
  );

  const isLoggedIn = Boolean(accessToken);
  const isAdmin = currentUserRole === "Admin";
  const hasAiAccess = currentUserRole === "Admin" || currentUserRole === "AiUser";

  return (
    <Routes>
      <Route path="/" element={<Navigate to="/login" replace />} />
      <Route
        path="/login"
        element={
          <main className="auth-page">
            <section className="auth-card">
              <h1>Вход</h1>
              <p className="subtitle">Авторизуйся для доступа к приложению</p>
              <label>Username</label>
              <input value={username} onChange={(e) => setUsername(e.target.value)} />
              <label>Password</label>
              <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" />
              <button onClick={() => loginAndStore(navigate)}>Войти</button>
              <p className="auth-link">Нет аккаунта? <Link to="/register">Регистрация</Link></p>
            </section>
          </main>
        }
      />
      <Route
        path="/register"
        element={
          <main className="auth-page">
            <section className="auth-card">
              <h1>Регистрация</h1>
              <p className="subtitle">Создай новый аккаунт пользователя</p>
              <label>Username</label>
              <input value={registerUsername} onChange={(e) => setRegisterUsername(e.target.value)} />
              <label>Password</label>
              <input value={registerPassword} onChange={(e) => setRegisterPassword(e.target.value)} type="password" />
              <button
                onClick={async () => {
                  const result = await request("POST", "/auth/register", { username: registerUsername.trim(), password: registerPassword });
                  handleResult(result);
                  if (result.ok) navigate("/login");
                }}
              >
                Зарегистрироваться
              </button>
              <p className="auth-link">Уже есть аккаунт? <Link to="/login">Вход</Link></p>
            </section>
          </main>
        }
      />
      <Route
        path="/app"
        element={
          !isLoggedIn ? <Navigate to="/login" replace /> : (
            <main className="app">
              <TopNav
                tab={tab}
                currentUserName={currentUserName || username || "unknown"}
                isAdmin={isAdmin}
                hasAiAccess={hasAiAccess}
                onTabChange={(id) => {
                  if (id === "admin" && !isAdmin) return;
                  if (id === "ai" && !hasAiAccess) return;
                  setTab(id);
                  if (id === "profile") {
                    loadMyProfile();
                  }
                  if (id === "workouts") {
                    loadWorkoutSessions();
                    loadWorkoutExerciseCatalog();
                  }
                  if (id === "admin") {
                    loadUsers();
                    loadAdminDialogs();
                  }
                }}
                onLogout={() => {
                  setAccessToken("");
                  setAdminToken("");
                  setAiToken("");
                  setCurrentUserName("");
                  setCurrentUserRole("");
                  setProfileBirthDate("");
                  setProfileHeightCm("");
                  setProfileWeightKg("");
                  setProfilePhone("");
                  setProfileCity("");
                  setProfileAbout("");
                  navigate("/login");
                }}
              />

        {tab === "ai" && hasAiAccess && <section className="card-grid">
          <section className="card">
            <h3>AI Chat</h3>
            <div className="row">
              <select value={currentDialogId} onChange={(e) => { setCurrentDialogId(e.target.value); loadDialogMessages(e.target.value); }}>
                <option value="">No dialogs</option>
                {dialogOptions}
              </select>
              <button onClick={async () => { const title = window.prompt("Введите название диалога:", "Новый диалог") || ""; const r = await request("POST", "/ai/dialogs", { title }, aiToken); handleResult(r); if (r.ok) await loadDialogs(aiToken, r.data?.id); }}>New</button>
              <button onClick={async () => { if (!currentDialogId) return; const current = dialogs.find((d) => d.id === currentDialogId); const title = window.prompt("Новое название:", current?.title || "Новый диалог"); if (!title) return; const r = await request("PUT", `/ai/dialogs/${currentDialogId}`, { title }, aiToken); handleResult(r); if (r.ok) await loadDialogs(aiToken, currentDialogId); }}>Rename</button>
              <button onClick={async () => { if (!currentDialogId) return; if (!window.confirm("Удалить диалог?")) return; const r = await request("DELETE", `/ai/dialogs/${currentDialogId}`, null, aiToken); handleResult(r); if (r.ok) await loadDialogs(aiToken); }}>Delete</button>
            </div>
            <div className="chat-messages">
              {chatMessages.map((m, i) => <div key={i} className={`chat-msg ${m.role === "user" ? "user" : "assistant"}`}>{m.content}</div>)}
            </div>
            <div className="row">
              <input value={chatPrompt} onChange={(e) => setChatPrompt(e.target.value)} placeholder="Type your message..." />
              <button onClick={sendChat}>Send</button>
            </div>
          </section>
        </section>}

        {tab === "workouts" && <section className="card-grid">
          <section className="card full-span">
            <h3>Тренировки</h3>
            <p className="subtitle">Управляй программами, своими упражнениями и фактическими тренировками.</p>
            <div className="workouts-subtabs">
              <button className={workoutsSubTab === "programs" ? "top-nav-tab active" : "top-nav-tab"} onClick={() => setWorkoutsSubTab("programs")}>Программы</button>
              <button className={workoutsSubTab === "exercises" ? "top-nav-tab active" : "top-nav-tab"} onClick={() => setWorkoutsSubTab("exercises")}>Мои упражнения</button>
              <button className={workoutsSubTab === "my-workouts" ? "top-nav-tab active" : "top-nav-tab"} onClick={() => setWorkoutsSubTab("my-workouts")}>Мои тренировки</button>
            </div>
            <div className="row">
              <button className="ghost-btn" onClick={() => { loadWorkoutSessions(); loadWorkoutExerciseCatalog(); }}>Обновить список</button>
            </div>

            {workoutsSubTab === "programs" && (
              <>
                <div className="row">
                  <button onClick={openProgramCreateModal}>Новая программа</button>
                </div>

                <h4>Сохраненные программы</h4>
                <div className="workout-list">
                  {workoutPrograms.length === 0 && <div className="workout-empty">Пока нет программ.</div>}
                  {workoutPrograms.map((session) => (
                    <article key={session.id} className="workout-item">
                      <h4>{session.day}</h4>
                      <div className="workout-meta">
                        <span>Код: {session.sessionCode}</span>
                        <span>Упражнений: {session.exercises.length}</span>
                      </div>
                      <div className="row">
                        <button onClick={() => openProgramEditModal(session)}>Редактировать</button>
                        <button className="ghost-btn" onClick={() => { setSelectedProgramCode(session.sessionCode); setWorkoutsSubTab("my-workouts"); }}>Начать тренировку</button>
                        <button className="danger-btn" onClick={() => openProgramDeleteModal(session)}>Удалить</button>
                      </div>
                    </article>
                  ))}
                </div>
              </>
            )}

            {workoutsSubTab === "exercises" && (
              <>
                <p className="subtitle">Создай новое упражнение и используй его в программах и тренировках.</p>
                <div className="row">
                  <button onClick={() => setIsCreateExerciseModalOpen(true)}>Создать упражнение</button>
                </div>
                <div className="users-table-wrap">
                  <table className="users-table">
                    <thead>
                      <tr>
                        <th>Упражнение</th>
                        <th>Комментарий</th>
                        <th>Действия</th>
                      </tr>
                    </thead>
                    <tbody>
                      {workoutExerciseCatalog.length === 0 && (
                        <tr>
                          <td colSpan="3">Пока нет упражнений в базе.</td>
                        </tr>
                      )}
                      {workoutExerciseCatalog.map((exercise) => {
                        const parsedMeta = parseProgramExerciseMeta(exercise.meta);
                        return (
                          <tr key={exercise.id}>
                            <td>{exercise.name}</td>
                            <td>{!parsedMeta.isStructured ? parsedMeta.legacy || "-" : "-"}</td>
                            <td>
                              <button className="danger-btn" onClick={() => deleteCatalogExercise(exercise.id)}>Удалить</button>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </>
            )}

            {workoutsSubTab === "my-workouts" && (
              <>
                <label>Выбери программу</label>
                <select value={selectedProgramCode} onChange={(e) => setSelectedProgramCode(e.target.value)}>
                  <option value="">Нет выбранной программы</option>
                  {workoutPrograms.map((program) => (
                    <option key={program.id} value={program.sessionCode}>{program.day}</option>
                  ))}
                </select>
                <div className="row">
                  <button disabled={!selectedProgram} onClick={() => selectedProgram && startWorkoutFromProgram(selectedProgram)}>Начать тренировку</button>
                  <button className="ghost-btn" onClick={createWorkoutFromScratch}>Создать с нуля</button>
                </div>

                {currentWorkout && (
                  <div className="workout-exercises">
                    <h4>Текущая тренировка: {currentWorkout.day}</h4>
                    <label>Название тренировки</label>
                    <input
                      value={currentWorkout.day || ""}
                      onChange={(e) => updateCurrentWorkoutField("day", e.target.value)}
                      placeholder="Например: День ног"
                    />
                    <label>Заметки</label>
                    <textarea
                      value={currentWorkout.notes || ""}
                      onChange={(e) => updateCurrentWorkoutField("notes", e.target.value)}
                      rows={2}
                      placeholder="Опционально"
                    />
                    <div className="row">
                      <button className="ghost-btn" onClick={addExerciseToCurrentWorkout}>Добавить упражнение</button>
                    </div>
                    <div className="users-table-wrap">
                      <table className="users-table">
                        <thead>
                          <tr>
                            <th>Упражнение</th>
                            <th>Комментарий</th>
                            <th>Подходы</th>
                            <th>Действия</th>
                          </tr>
                        </thead>
                        <tbody>
                          {currentWorkout.exercises.length === 0 && (
                            <tr>
                              <td colSpan="4">Нет упражнений. Добавь первое упражнение.</td>
                            </tr>
                          )}
                          {currentWorkout.exercises.map((exercise) => (
                            <tr key={exercise.id}>
                              <td>
                                <input
                                  value={exercise.name || ""}
                                  onChange={(e) => updateCurrentWorkoutExercise(exercise.id, "name", e.target.value)}
                                  placeholder="Название упражнения"
                                />
                              </td>
                              <td>
                                <input
                                  value={exercise.meta || ""}
                                  onChange={(e) => updateCurrentWorkoutExercise(exercise.id, "meta", e.target.value)}
                                  placeholder="Комментарий к упражнению"
                                />
                              </td>
                              <td>
                                <div className="workout-sets">
                                  {exercise.sets.map((setItem, setIdx) => (
                                    <div key={`${exercise.id}-active-set-${setIdx}`} className="row workout-set-row">
                                      <input
                                        type="number"
                                        inputMode="decimal"
                                        min="0"
                                        step="0.5"
                                        value={setItem.weight}
                                        onChange={(e) => updateCurrentWorkoutSet(exercise.id, setIdx, "weight", e.target.value)}
                                        placeholder="Вес"
                                      />
                                      <input
                                        type="number"
                                        inputMode="numeric"
                                        min="0"
                                        step="1"
                                        value={setItem.reps}
                                        onChange={(e) => updateCurrentWorkoutSet(exercise.id, setIdx, "reps", e.target.value)}
                                        placeholder="Повт."
                                      />
                                      <select
                                        value={setItem.rpe || "8"}
                                        onChange={(e) => updateCurrentWorkoutSet(exercise.id, setIdx, "rpe", e.target.value)}
                                      >
                                        <option value="6">6</option>
                                        <option value="7">7</option>
                                        <option value="8">8</option>
                                        <option value="9">9</option>
                                        <option value="10">10</option>
                                      </select>
                                      <button
                                        className="danger-btn"
                                        onClick={() => removeCurrentWorkoutSet(exercise.id, setIdx)}
                                        disabled={exercise.sets.length <= 1}
                                      >
                                        Удалить
                                      </button>
                                    </div>
                                  ))}
                                </div>
                              </td>
                              <td>
                                <div className="row">
                                  <button className="ghost-btn" onClick={() => addCurrentWorkoutSet(exercise.id)}>+ Подход</button>
                                  <button className="danger-btn" onClick={() => removeCurrentWorkoutExercise(exercise.id)}>Удалить</button>
                                </div>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}

                <div className="row">
                  <button disabled={!currentWorkout} onClick={saveCurrentWorkout}>Сохранить выполненную тренировку</button>
                </div>

                <h4>История моих тренировок</h4>
                <div className="workout-list">
                  {workoutLogs.length === 0 && <div className="workout-empty">Пока нет сохраненных тренировок.</div>}
                  {workoutLogs.map((session) => (
                    <article key={session.id} className="workout-item">
                      <h4>{session.day}</h4>
                      <div className="workout-meta">
                        <span>Дата: {session.date || session._date || "-"}</span>
                        <span>Упражнений: {session.exercises.length}</span>
                      </div>
                      <div className="row">
                        <button className="danger-btn" onClick={() => deleteWorkoutSession(session.id)}>Удалить</button>
                      </div>
                    </article>
                  ))}
                </div>
              </>
            )}
          </section>
        </section>}

        {tab === "profile" && <section className="card-grid">
          <section className="card full-span">
            <h3>Мой профиль</h3>
            <p className="subtitle">Заполни дополнительные данные (все поля необязательные).</p>
            <div className="row profile-row">
              <div>
                <label>Дата рождения</label>
                <input type="date" value={profileBirthDate} onChange={(e) => setProfileBirthDate(e.target.value)} />
              </div>
              <div>
                <label>Рост (см)</label>
                <input
                  type="number"
                  value={profileHeightCm}
                  onChange={(e) => setProfileHeightCm(e.target.value)}
                  placeholder="например, 175"
                  min="0"
                  max="300"
                  step="0.01"
                />
              </div>
              <div>
                <label>Вес (кг)</label>
                <input
                  type="number"
                  value={profileWeightKg}
                  onChange={(e) => setProfileWeightKg(e.target.value)}
                  placeholder="например, 72.5"
                  min="0"
                  max="700"
                  step="0.01"
                />
              </div>
            </div>
            <label>Телефон</label>
            <input value={profilePhone} onChange={(e) => setProfilePhone(e.target.value)} placeholder="+7..." />
            <label>Город</label>
            <input value={profileCity} onChange={(e) => setProfileCity(e.target.value)} placeholder="Алматы" />
            <label>О себе</label>
            <textarea
              value={profileAbout}
              onChange={(e) => setProfileAbout(e.target.value)}
              placeholder="Любая полезная информация"
              rows={4}
            />
            <div className="row">
              <button onClick={saveMyProfile}>Сохранить профиль</button>
              <button className="ghost-btn" onClick={() => loadMyProfile()}>Обновить</button>
            </div>
          </section>
        </section>}

        {tab === "admin" && isAdmin && <section className="card-grid">
          <section className="card full-span">
            <h3>Управление пользователем</h3>
            <div className="row">
              <button onClick={() => setIsCreateUserModalOpen(true)}>New User</button>
              <button onClick={loadUsers}>Reload Users</button>
            </div>
            <div className="users-table-wrap">
              <table className="users-table">
                <thead>
                  <tr>
                    <th>Username</th>
                    <th>Role</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {users.length === 0 && <tr><td colSpan="3">No users loaded</td></tr>}
                  {users.map((u) => (
                    <tr key={`manage-${u.id}`}>
                      <td>{u.username}</td>
                      <td>{u.role}</td>
                      <td className="admin-actions">
                        <button className="ghost-btn" onClick={() => setAdminManageUserId(u.id)}>Select</button>
                        <button onClick={() => openEditModal(u)}>Edit Role/Username</button>
                        <button onClick={() => openPasswordModal(u)}>Password</button>
                        <button className="danger-btn" onClick={() => openDeleteModal(u)}>Delete</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          <section className="card full-span">
            <h3>Пользователи</h3>
            <div className="users-table-wrap">
              <table className="users-table">
                <thead>
                  <tr>
                    <th>Username</th>
                    <th>Role</th>
                    <th>Birth Date</th>
                    <th>Height (cm)</th>
                    <th>Weight (kg)</th>
                    <th>Phone</th>
                    <th>City</th>
                    <th>About</th>
                    <th>Created</th>
                    <th>User ID</th>
                  </tr>
                </thead>
                <tbody>
                  {users.length === 0 && <tr><td colSpan="10">No users loaded</td></tr>}
                  {users.map((u) => (
                    <tr key={u.id}>
                      <td>{u.username}</td>
                      <td>{u.role}</td>
                      <td>{u.birthDate || "-"}</td>
                      <td>{u.heightCm ?? "-"}</td>
                      <td>{u.weightKg ?? "-"}</td>
                      <td>{u.phone || "-"}</td>
                      <td>{u.city || "-"}</td>
                      <td>{u.about || "-"}</td>
                      <td>{u.createdAtUtc ? new Date(u.createdAtUtc).toLocaleString() : "-"}</td>
                      <td>{u.id}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <p className="subtitle">Режим только чтение: редактирование данных пользователя для администратора отключено.</p>
          </section>

          <section className="card full-span">
            <h3>Диалоги (Admin)</h3>
            <label>Пользователь</label>
            <div className="row">
              <select
                value={adminDialogUserId}
                onChange={(e) => {
                  const nextUserId = e.target.value;
                  setAdminDialogUserId(nextUserId);
                  setAdminCurrentDialogId("");
                  setAdminDialogMessages([]);
                  loadAdminDialogs(nextUserId);
                }}
              >
                <option value="">All users</option>
                {adminUserOptions}
              </select>
            </div>

            <label>Диалог</label>
            <div className="row">
              <select value={adminCurrentDialogId} onChange={(e) => { setAdminCurrentDialogId(e.target.value); loadAdminDialogMessages(e.target.value); }}>
                <option value="">No dialogs</option>
                {adminDialogOptions}
              </select>
              <button onClick={renameAdminDialog}>Rename</button>
              <button className="danger-btn" onClick={deleteAdminDialog}>Delete</button>
            </div>

            <div className="chat-messages small">
              {adminDialogMessages.length === 0 && <div className="chat-msg assistant">Нет сообщений для выбранного диалога.</div>}
              {adminDialogMessages.map((m) => (
                <div key={m.id} className={`chat-msg ${m.role === "user" ? "user" : "assistant"}`}>
                  {m.content}
                </div>
              ))}
            </div>
          </section>
        </section>}

        {tab === "admin" && isAdmin && isCreateUserModalOpen && (
          <div className="modal-backdrop">
            <div className="modal-card">
              <h3>Создать пользователя</h3>
              <label>Username</label>
              <input
                value={adminCreateUsername}
                onChange={(e) => setAdminCreateUsername(e.target.value)}
                placeholder="username"
              />
              <label>Password</label>
              <input
                value={adminCreatePassword}
                onChange={(e) => setAdminCreatePassword(e.target.value)}
                placeholder="password"
                type="password"
              />
              <label>Role</label>
              <select value={adminCreateRole} onChange={(e) => setAdminCreateRole(e.target.value)}>
                <option>User</option><option>AiUser</option><option>Admin</option>
              </select>
              <div className="row">
                <button onClick={createAdminUser}>Create</button>
                <button className="ghost-btn" onClick={() => setIsCreateUserModalOpen(false)}>Cancel</button>
              </div>
            </div>
          </div>
        )}
        {tab === "admin" && isAdmin && isEditUserModalOpen && selectedAdminUser && (
          <div className="modal-backdrop">
            <div className="modal-card">
              <h3>Редактировать пользователя</h3>
              <label>Username</label>
              <input value={editUserName} onChange={(e) => setEditUserName(e.target.value)} />
              <label>Role</label>
              <select value={editUserRole} onChange={(e) => setEditUserRole(e.target.value)}>
                <option>User</option><option>AiUser</option><option>Admin</option>
              </select>
              <div className="row">
                <button onClick={saveAdminUserFromModal}>Save</button>
                <button className="ghost-btn" onClick={() => setIsEditUserModalOpen(false)}>Cancel</button>
              </div>
            </div>
          </div>
        )}
        {tab === "admin" && isAdmin && isPasswordModalOpen && selectedAdminUser && (
          <div className="modal-backdrop">
            <div className="modal-card">
              <h3>Сменить пароль</h3>
              <p className="subtitle">{selectedAdminUser.username}</p>
              <label>New Password</label>
              <input
                value={newPasswordValue}
                onChange={(e) => setNewPasswordValue(e.target.value)}
                type="password"
                placeholder="new password"
              />
              <div className="row">
                <button onClick={saveAdminPasswordFromModal}>Save Password</button>
                <button className="ghost-btn" onClick={() => setIsPasswordModalOpen(false)}>Cancel</button>
              </div>
            </div>
          </div>
        )}
        {tab === "admin" && isAdmin && isDeleteModalOpen && selectedAdminUser && (
          <div className="modal-backdrop">
            <div className="modal-card">
              <h3>Удалить пользователя</h3>
              <p className="subtitle">Ты точно хочешь удалить: <b>{selectedAdminUser.username}</b>?</p>
              <div className="row">
                <button className="danger-btn" onClick={deleteAdminUserFromModal}>Delete</button>
                <button className="ghost-btn" onClick={() => setIsDeleteModalOpen(false)}>Cancel</button>
              </div>
            </div>
          </div>
        )}

        {tab === "workouts" && workoutsSubTab === "programs" && isProgramModalOpen && (
          <div className="modal-backdrop">
            <div className="modal-card">
              <h3>{editingProgramId ? "Редактирование программы" : "Новая программа"}</h3>
              <label>Код программы (латиницей, опционально)</label>
              <input value={programCode} onChange={(e) => setProgramCode(e.target.value)} placeholder="upper-body-a" />
              <label>Название программы</label>
              <input value={programDay} onChange={(e) => setProgramDay(e.target.value)} placeholder="Верх тела A" />
              <label>Заметки</label>
              <textarea value={programNotes} onChange={(e) => setProgramNotes(e.target.value)} rows={3} placeholder="Комментарий к программе" />
              <div className="row">
                <select value={selectedCatalogExerciseId} onChange={(e) => setSelectedCatalogExerciseId(e.target.value)}>
                  <option value="">Выбери упражнение из общего списка</option>
                  {workoutExerciseCatalog.map((exercise) => (
                    <option key={exercise.id} value={exercise.id}>{exercise.name}</option>
                  ))}
                </select>
                <button onClick={() => selectedCatalogExercise && addExerciseToProgram(selectedCatalogExercise)} disabled={!selectedCatalogExercise}>Добавить в программу</button>
              </div>
              <div className="workout-exercises">
                {programExercisesDraft.map((exercise) => (
                  <div key={exercise.id} className="workout-exercise">
                    <p className="workout-exercise-name">{exercise.name}</p>
                    <label>Комментарий к упражнению</label>
                    <textarea
                      value={exercise.comment || ""}
                      onChange={(e) => updateProgramExerciseComment(exercise.id, e.target.value)}
                      rows={2}
                      placeholder="Например: техника, темп, акцент, ограничения"
                    />
                    <div className="row">
                      <button className="danger-btn" onClick={() => removeProgramExercise(exercise.id)}>Удалить упражнение</button>
                    </div>
                  </div>
                ))}
              </div>
              <div className="row">
                <button onClick={saveProgramToDb}>Сохранить программу</button>
                <button className="ghost-btn" onClick={closeProgramModal}>Отмена</button>
              </div>
            </div>
          </div>
        )}

        {tab === "workouts" && workoutsSubTab === "programs" && isProgramDeleteModalOpen && pendingDeleteProgram && (
          <div className="modal-backdrop">
            <div className="modal-card">
              <h3>Удалить программу</h3>
              <p className="subtitle">Ты точно хочешь удалить программу: <b>{pendingDeleteProgram.day || pendingDeleteProgram.sessionCode}</b>?</p>
              <div className="row">
                <button className="danger-btn" onClick={deleteProgramFromModal}>Удалить</button>
                <button
                  className="ghost-btn"
                  onClick={() => {
                    setIsProgramDeleteModalOpen(false);
                    setPendingDeleteProgram(null);
                  }}
                >
                  Отмена
                </button>
              </div>
            </div>
          </div>
        )}
        {tab === "workouts" && workoutsSubTab === "exercises" && isCreateExerciseModalOpen && (
          <div className="modal-backdrop">
            <div className="modal-card">
              <h3>Создать упражнение</h3>
              <label>Название упражнения</label>
              <input
                value={newCatalogExerciseName}
                onChange={(e) => setNewCatalogExerciseName(e.target.value)}
                placeholder="Например: Жим лежа"
              />
              <label>Комментарий (опционально)</label>
              <input
                value={newCatalogExerciseMeta}
                onChange={(e) => setNewCatalogExerciseMeta(e.target.value)}
                placeholder="Например: гриф + 20 кг"
              />
              <div className="row">
                <button onClick={createCatalogExercise}>Создать</button>
                <button className="ghost-btn" onClick={() => setIsCreateExerciseModalOpen(false)}>Отмена</button>
              </div>
            </div>
          </div>
        )}

            </main>
          )
        }
      />
    </Routes>
  );
}

function AppShell() {
  return (
    <BrowserRouter>
      <AppContent />
    </BrowserRouter>
  );
}

export default AppShell;
