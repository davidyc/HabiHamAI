import { useEffect, useMemo, useRef, useState } from 'react';
import {
  BrowserRouter,
  Link,
  Navigate,
  Route,
  Routes,
  useNavigate,
} from 'react-router-dom';
import TopNav from './TopNav';
import ModalShell from './shared/ui/ModalShell';
import BikeTrackMap from './shared/ui/BikeTrackMap';
import FilterSelect, {
  DatePeriodFilter,
  HABIT_DATE_PERIOD_OPTIONS,
  STANDARD_DATE_PERIOD_OPTIONS,
  TODO_DATE_PERIOD_OPTIONS,
} from './shared/ui/FilterSelect';
import {
  SegmentTab,
  SegmentTabs,
  SubNavGroup,
  SubNavTab,
} from './shared/ui/SegmentTabs';
import {
  computeHabitPeriodAnalytics,
  computeTodoPeriodAnalytics,
  isTodoOverdue,
} from './shared/tracking/analytics';
import {
  HabitPeriodAnalyticsPanel,
  TodoPeriodAnalyticsPanel,
} from './shared/ui/TrackingAnalyticsPanel';

function AppContent() {
  const getTodayIsoDate = () => new Date().toISOString().slice(0, 10);
  const getIsoDateDaysAgo = (daysAgo) => {
    const date = new Date();
    date.setDate(date.getDate() - daysAgo);
    return date.toISOString().slice(0, 10);
  };
  /** Последние 7 дней включая сегодня (как пресет «Неделя»). */
  const getRollingWeekRange = () => ({
    from: getIsoDateDaysAgo(6),
    to: getTodayIsoDate(),
  });
  const APP_PERMISSION = {
    Workouts: 'app.workouts',
    Bike: 'app.bike',
    Progress: 'app.progress',
    Habits: 'app.habits',
    Todos: 'app.todos',
    Profile: 'app.profile',
    Investments: 'app.investments',
    AiAssistant: 'ai.assistant',
    AdminUsers: 'admin.users',
    AdminRoles: 'admin.roles',
    AdminCategories: 'admin.categories',
    AdminProfiles: 'admin.profiles',
    AdminAiAssistants: 'admin.ai_assistants',
    AdminAiTestChat: 'admin.ai_test_chat',
    AdminDialogs: 'admin.dialogs',
  };
  const EXERCISE_MUSCLE_GROUP_OPTIONS = [
    'Грудь',
    'Спина',
    'Ноги',
    'Плечи',
    'Руки',
    'Пресс',
    'Ягодицы',
  ];
  const ADMIN_SUBTAB_PERMISSION = {
    'users-manage': APP_PERMISSION.AdminUsers,
    roles: APP_PERMISSION.AdminRoles,
    categories: APP_PERMISSION.AdminCategories,
    'users-profiles': APP_PERMISSION.AdminProfiles,
    'ai-assistants': APP_PERMISSION.AdminAiAssistants,
    'ai-test-chat': APP_PERMISSION.AdminAiTestChat,
    dialogs: APP_PERMISSION.AdminDialogs,
  };
  const PERMISSION_CATEGORY_LABELS = {
    app: 'Приложение',
    ai: 'AI',
    admin: 'Админка',
  };
  const normalizeUserRoles = (source) => {
    if (Array.isArray(source?.roles) && source.roles.length > 0) {
      return source.roles.map((role) => String(role));
    }
    if (source?.role) {
      return [String(source.role)];
    }
    return ['User'];
  };
  const userHasAnyRole = (roles, ...expected) => {
    const set = new Set(roles.map((role) => String(role).toLowerCase()));
    return expected.some((role) => set.has(String(role).toLowerCase()));
  };
  const formatUserDisplayName = (firstName, lastName, loginFallback) => {
    const fullName = [firstName, lastName]
      .map((part) => String(part ?? '').trim())
      .filter(Boolean)
      .join(' ');
    if (fullName) return fullName;
    const login = String(loginFallback ?? '').trim();
    return login || 'неизвестно';
  };
  const getIsoDateRange = (fromIso, toIso) => {
    if (!fromIso || !toIso) return [];
    const start = new Date(`${fromIso}T00:00:00Z`);
    const end = new Date(`${toIso}T00:00:00Z`);
    if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()))
      return [];
    const res = [];
    const cursor = new Date(start);
    while (cursor.getTime() <= end.getTime()) {
      res.push(cursor.toISOString().slice(0, 10));
      cursor.setUTCDate(cursor.getUTCDate() + 1);
    }
    return res;
  };
  const habitCheckinDateKey = (entry) => {
    if (!entry) return '';
    const d = entry.date ?? entry.Date;
    return typeof d === 'string' ? d.slice(0, 10) : '';
  };
  const habitCheckinStatusKey = (entry) =>
    String(entry?.status ?? entry?.Status ?? '').toLowerCase();
  const buildHabitCheckinsMap = (entries) => {
    const map = {};
    if (!Array.isArray(entries)) return map;
    for (const entry of entries) {
      const date = habitCheckinDateKey(entry);
      const status = habitCheckinStatusKey(entry);
      if (date && status) map[date] = status;
    }
    return map;
  };
  const HABIT_STATUS_CYCLE = [null, 'partial', 'done', 'failed'];
  const nextHabitCheckinStatus = (current) => {
    const normalized = current || null;
    const idx = HABIT_STATUS_CYCLE.indexOf(normalized);
    const nextIdx = idx < 0 ? 0 : (idx + 1) % HABIT_STATUS_CYCLE.length;
    return HABIT_STATUS_CYCLE[nextIdx];
  };
  const habitStatusCellClass = (status) => {
    if (status === 'partial') {
      return 'habit-status-cell habit-status-cell--partial';
    }
    if (status === 'done') {
      return 'habit-status-cell habit-status-cell--done';
    }
    if (status === 'failed') {
      return 'habit-status-cell habit-status-cell--failed';
    }
    return 'habit-status-cell habit-status-cell--none';
  };
  const isHabitMastered = (habit) => Boolean(habit?.isMastered);
  const habitMasteryLabel = (habit) => {
    if (habit?.isMastered) return 'Освоена';
    const target = Number(habit?.daysToMaster ?? 0);
    if (!target) return '—';
    const streak = Number(habit?.currentStreakDays ?? 0);
    return `${streak} / ${target} дн.`;
  };
  const habitStatusLabel = (status) => {
    if (status === 'partial') return 'частично';
    if (status === 'done') return 'выполнено';
    if (status === 'failed') return 'провалено';
    return 'без отметки';
  };
  const resolveHabitStatusForDate = (statusMap, habit, date, habitDateField) => {
    if (statusMap?.[date]) return statusMap[date];
    const today = getTodayIsoDate();
    if (date !== today || !habitDateField) return null;
    const fromHabit = String(
      habit?.[habitDateField] ?? habit?.TodayStatus ?? '',
    ).toLowerCase();
    return fromHabit || null;
  };
  const resolveHabitTodayStatus = (statusMap, habit, today) =>
    resolveHabitStatusForDate(statusMap, habit, today, 'todayStatus');
  /** Прошлая календарная неделя (пн–вс) в UTC — как на сервере для endingOn. */
  const getPreviousCalendarWeekUtc = () => {
    const now = new Date();
    const today = new Date(
      Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()),
    );
    const dow = today.getUTCDay();
    const daysSinceMonday = (dow + 6) % 7;
    const prevSunday = new Date(today);
    prevSunday.setUTCDate(today.getUTCDate() - daysSinceMonday - 1);
    const prevMonday = new Date(prevSunday);
    prevMonday.setUTCDate(prevSunday.getUTCDate() - 6);
    const fmt = (d) => d.toISOString().slice(0, 10);
    return {
      periodFrom: fmt(prevMonday),
      periodTo: fmt(prevSunday),
      endingOn: fmt(prevSunday),
      days: 7,
    };
  };
  const getUtcTodayDate = () => {
    const now = new Date();
    return new Date(
      Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()),
    );
  };
  const getCurrentCalendarMonthUtc = () => {
    const now = new Date();
    const year = now.getUTCFullYear();
    const month = now.getUTCMonth();
    const first = new Date(Date.UTC(year, month, 1));
    const last = new Date(Date.UTC(year, month + 1, 0));
    const fmt = (d) => d.toISOString().slice(0, 10);
    return { from: fmt(first), to: fmt(last) };
  };
  /** Ближайшие предстоящие выходные (сб–вс) в UTC. */
  const getWeekendRangeUtc = () => {
    const today = getUtcTodayDate();
    const dow = today.getUTCDay();
    let daysUntilSaturday;
    if (dow === 6) {
      daysUntilSaturday = 0;
    } else if (dow === 0) {
      daysUntilSaturday = 6;
    } else {
      daysUntilSaturday = 6 - dow;
    }
    const saturday = new Date(today);
    saturday.setUTCDate(today.getUTCDate() + daysUntilSaturday);
    const sunday = new Date(saturday);
    sunday.setUTCDate(saturday.getUTCDate() + 1);
    const fmt = (d) => d.toISOString().slice(0, 10);
    return { from: fmt(saturday), to: fmt(sunday) };
  };

  const navigate = useNavigate();
  const [tab, setTab] = useState('workouts');
  const [baseUrl, setBaseUrl] = useState(
    import.meta.env.VITE_API_BASE_URL || 'http://localhost:5193',
  );
  const [accessToken, setAccessToken] = useState('');
  const [adminToken, setAdminToken] = useState('');
  const [aiToken, setAiToken] = useState('');
  const [currentUserName, setCurrentUserName] = useState('');
  const [currentUserRoles, setCurrentUserRoles] = useState([]);
  const [currentUserPermissions, setCurrentUserPermissions] = useState([]);
  const [errorView, setErrorView] = useState('Ошибок нет.');

  const [registerUsername, setRegisterUsername] = useState('user1');
  const [registerPassword, setRegisterPassword] = useState('user1234');
  const [registerError, setRegisterError] = useState('');
  const [username, setUsername] = useState('admin');
  const [password, setPassword] = useState('admin123');
  const [loginError, setLoginError] = useState('');
  const [isLoginLoading, setIsLoginLoading] = useState(false);
  const [activeApiRequests, setActiveApiRequests] = useState(0);

  const [dialogs, setDialogs] = useState([]);
  const [currentDialogId, setCurrentDialogId] = useState('');
  const [chatPrompt, setChatPrompt] = useState('');
  const [chatMessages, setChatMessages] = useState([
    {
      role: 'assistant',
      content: 'Привет! Войди через форму входа и отправь сообщение.',
    },
  ]);
  const [weeklyTrainingReviews, setWeeklyTrainingReviews] = useState([]);
  const [isWeeklyReviewModalOpen, setIsWeeklyReviewModalOpen] =
    useState(false);
  const [selectedWeeklyReview, setSelectedWeeklyReview] = useState(null);
  const [weeklyReviewModalLoading, setWeeklyReviewModalLoading] =
    useState(false);
  const [weeklyReviewGenerating, setWeeklyReviewGenerating] = useState(false);
  const [pendingDeleteWeeklyReview, setPendingDeleteWeeklyReview] =
    useState(null);
  const [workoutSessions, setWorkoutSessions] = useState([]);
  const [workoutsSubTab, setWorkoutsSubTab] = useState('strength');
  const [strengthSubTab, setStrengthSubTab] = useState('manage');
  const [workoutsManageSubTab, setWorkoutsManageSubTab] = useState('add');

  function openStrengthSubTab(sub) {
    setWorkoutsSubTab('strength');
    setStrengthSubTab(sub);
  }

  const [progressSubTab, setProgressSubTab] = useState('weight-tracker');
  const [adminSubTab, setAdminSubTab] = useState('users-manage');
  const [workoutExerciseCatalog, setWorkoutExerciseCatalog] = useState([]);
  const [selectedCatalogExerciseId, setSelectedCatalogExerciseId] =
    useState('');
  const [newCatalogExerciseName, setNewCatalogExerciseName] = useState('');
  const [newCatalogExerciseMeta, setNewCatalogExerciseMeta] = useState('');
  const [newCatalogExerciseMuscleGroup, setNewCatalogExerciseMuscleGroup] =
    useState('');
  const [exerciseCatalogSearch, setExerciseCatalogSearch] = useState('');
  const [exerciseCatalogSort, setExerciseCatalogSort] = useState({
    key: 'name',
    dir: 'asc',
  });
  const [isCreateExerciseModalOpen, setIsCreateExerciseModalOpen] =
    useState(false);
  const [editingCatalogExerciseId, setEditingCatalogExerciseId] = useState('');
  const [programCode, setProgramCode] = useState('');
  const [programDay, setProgramDay] = useState('');
  const [programNotes, setProgramNotes] = useState('');
  const [programExercisesDraft, setProgramExercisesDraft] = useState([]);
  const [programExercisePickerSearch, setProgramExercisePickerSearch] =
    useState('');
  const [programExercisePickerOpen, setProgramExercisePickerOpen] =
    useState(false);
  const [isProgramModalOpen, setIsProgramModalOpen] = useState(false);
  const [isProgramDeleteModalOpen, setIsProgramDeleteModalOpen] =
    useState(false);
  const [editingProgramId, setEditingProgramId] = useState('');
  const [pendingDeleteProgram, setPendingDeleteProgram] = useState(null);
  const [selectedProgramCode, setSelectedProgramCode] = useState('');
  const [currentWorkout, setCurrentWorkout] = useState(null);
  const [profileBirthDate, setProfileBirthDate] = useState('');
  const [profileHeightCm, setProfileHeightCm] = useState('');
  const [profileWeightKg, setProfileWeightKg] = useState('');
  const [profilePhone, setProfilePhone] = useState('');
  const [profileCity, setProfileCity] = useState('');
  const [profileAbout, setProfileAbout] = useState('');
  const [profileFirstName, setProfileFirstName] = useState('');
  const [profileLastName, setProfileLastName] = useState('');
  const [profileAiSummary, setProfileAiSummary] = useState('');
  const [profileTelegramLinked, setProfileTelegramLinked] = useState(false);
  const [isTelegramLinkModalOpen, setIsTelegramLinkModalOpen] = useState(false);
  const [telegramLinkUrl, setTelegramLinkUrl] = useState('');
  const [telegramLinkExpiresAt, setTelegramLinkExpiresAt] = useState('');
  const [telegramLinkLoading, setTelegramLinkLoading] = useState(false);
  const [telegramLinkError, setTelegramLinkError] = useState('');
  const [isTelegramUnlinkConfirmOpen, setIsTelegramUnlinkConfirmOpen] =
    useState(false);
  const [weightTrackerEntries, setWeightTrackerEntries] = useState([]);
  const [weightTrackerDate, setWeightTrackerDate] = useState(() =>
    getTodayIsoDate(),
  );
  const [weightTrackerValue, setWeightTrackerValue] = useState('');
  /** Фильтр периода для таблицы/графика/выгрузки (по умолчанию — неделя). */
  const [weightDatePeriodPreset, setWeightDatePeriodPreset] = useState('7');
  const [weightFilterDateFrom, setWeightFilterDateFrom] = useState(
    () => getRollingWeekRange().from,
  );
  const [weightFilterDateTo, setWeightFilterDateTo] = useState(
    () => getRollingWeekRange().to,
  );
  const [pendingDeleteWeightEntry, setPendingDeleteWeightEntry] =
    useState(null);

  // Прогресс: привычки
  const [userCategories, setUserCategories] = useState([]);
  const [habitsOverview, setHabitsOverview] = useState([]);
  const [habitsNewName, setHabitsNewName] = useState('');
  const [habitsNewCategoryId, setHabitsNewCategoryId] = useState('');
  const [habitsNewDaysToMaster, setHabitsNewDaysToMaster] = useState('21');
  const [habitDatePeriodPreset, setHabitDatePeriodPreset] = useState('7');
  const [habitFilterDateFrom, setHabitFilterDateFrom] = useState(
    () => getRollingWeekRange().from,
  );
  const [habitFilterDateTo, setHabitFilterDateTo] = useState(
    () => getRollingWeekRange().to,
  );
  /** habitId → Set of ISO dates with check-in */
  const [habitsCheckinsByHabitId, setHabitsCheckinsByHabitId] = useState({});
  const [isCreateHabitModalOpen, setIsCreateHabitModalOpen] = useState(false);
  const [editingHabit, setEditingHabit] = useState(null);
  const [habitsEditName, setHabitsEditName] = useState('');
  const [habitsEditCategoryId, setHabitsEditCategoryId] = useState('');
  const [habitsEditDaysToMaster, setHabitsEditDaysToMaster] = useState('21');
  const [pendingDeleteHabit, setPendingDeleteHabit] = useState(null);
  const [pendingDeleteTodo, setPendingDeleteTodo] = useState(null);
  const [pendingCompleteTodo, setPendingCompleteTodo] = useState(null);
  const [habitsCategoryTabKey, setHabitsCategoryTabKey] = useState('__all__');

  // Прогресс: задачи/ToDo
  const [todos, setTodos] = useState([]);
  const [todoTitleDraft, setTodoTitleDraft] = useState('');
  const [todoDueDateDraft, setTodoDueDateDraft] = useState('');
  const [todoCategoryDraft, setTodoCategoryDraft] = useState('');
  const [isCreateTodoModalOpen, setIsCreateTodoModalOpen] = useState(false);
  const [todoDatePeriodPreset, setTodoDatePeriodPreset] = useState('week');
  const [todoFilterDateFrom, setTodoFilterDateFrom] = useState(
    () => getRollingWeekRange().from,
  );
  const [todoFilterDateTo, setTodoFilterDateTo] = useState(
    () => getRollingWeekRange().to,
  );
  const [todosCategoryTabKey, setTodosCategoryTabKey] = useState('__all__');
  const [todoTableSort, setTodoTableSort] = useState({ key: '', dir: 'asc' });
  const [todoStatusFilter, setTodoStatusFilter] = useState('all');

  const [users, setUsers] = useState([]);
  const [adminCreateUsername, setAdminCreateUsername] = useState('');
  const [adminCreatePassword, setAdminCreatePassword] = useState('');
  const [adminCreateRoles, setAdminCreateRoles] = useState(['AiUser']);
  const [isCreateUserModalOpen, setIsCreateUserModalOpen] = useState(false);
  const [isEditUserModalOpen, setIsEditUserModalOpen] = useState(false);
  const [isPasswordModalOpen, setIsPasswordModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [selectedAdminUser, setSelectedAdminUser] = useState(null);
  const [adminManageUserId, setAdminManageUserId] = useState('');
  const [editUserName, setEditUserName] = useState('');
  const [editUserRoles, setEditUserRoles] = useState(['User']);
  const [newPasswordValue, setNewPasswordValue] = useState('');
  const [adminDialogUserId, setAdminDialogUserId] = useState('');
  const [adminDialogs, setAdminDialogs] = useState([]);
  const [adminCurrentDialogId, setAdminCurrentDialogId] = useState('');
  const [adminDialogMessages, setAdminDialogMessages] = useState([]);
  const [isProfileEditModalOpen, setIsProfileEditModalOpen] = useState(false);
  const [isActiveWorkoutModalOpen, setIsActiveWorkoutModalOpen] =
    useState(false);
  const [aiDialogModalKind, setAiDialogModalKind] = useState(null);
  const [aiDialogTitleDraft, setAiDialogTitleDraft] = useState('');
  const [adminDialogModalKind, setAdminDialogModalKind] = useState(null);
  const [adminDialogTitleDraft, setAdminDialogTitleDraft] = useState('');
  const [aiAssistants, setAiAssistants] = useState([]);
  /** Пробный чат с помощником без смены «Включить» (передаётся assistantId в POST /ai/chat); задаётся из админки. */
  const [chatAssistantPreviewId, setChatAssistantPreviewId] = useState(null);
  const adminAssistantTestChatPanelRef = useRef(null);
  const [adminAiAssistants, setAdminAiAssistants] = useState([]);
  const [assistantModalKind, setAssistantModalKind] = useState(null);
  const [assistantDraft, setAssistantDraft] = useState({
    id: '',
    name: '',
    description: '',
    systemPrompt: '',
    settingsJson: '',
    sortOrder: 0,
    isActive: true,
  });
  const [pendingDeleteAssistantId, setPendingDeleteAssistantId] =
    useState(null);
  const [isAiExtraInfoModalOpen, setIsAiExtraInfoModalOpen] = useState(false);
  const [aiExtraInfoAssistantId, setAiExtraInfoAssistantId] = useState('');
  const [aiExtraInfoDefinitions, setAiExtraInfoDefinitions] = useState([]);
  const [aiExtraInfoValues, setAiExtraInfoValues] = useState({});
  const [adminExtraFieldsList, setAdminExtraFieldsList] = useState([]);
  const [adminExtraFieldModalKind, setAdminExtraFieldModalKind] =
    useState(null);
  const [adminExtraFieldDraft, setAdminExtraFieldDraft] = useState({
    id: '',
    fieldKey: '',
    label: '',
    fieldType: 'text',
    sortOrder: 0,
    isRequired: false,
  });
  const [pendingDeleteExtraField, setPendingDeleteExtraField] = useState(null);
  const [adminCategories, setAdminCategories] = useState([]);
  const [adminCategoryModalKind, setAdminCategoryModalKind] = useState(null);
  const [adminCategoryDraft, setAdminCategoryDraft] = useState({
    id: '',
    name: '',
    description: '',
    isActive: true,
    sortOrder: 0,
  });
  const [pendingDeleteCategory, setPendingDeleteCategory] = useState(null);
  const [adminRoles, setAdminRoles] = useState([]);
  const [adminRoleModalKind, setAdminRoleModalKind] = useState(null);
  const [adminRoleDraft, setAdminRoleDraft] = useState({
    id: '',
    name: '',
    label: '',
    description: '',
    isActive: true,
    sortOrder: 0,
    isSystem: false,
  });
  const [pendingDeleteRole, setPendingDeleteRole] = useState(null);
  const [adminPermissionsCatalog, setAdminPermissionsCatalog] = useState([]);
  const [rolePermissionsModalRole, setRolePermissionsModalRole] = useState(null);
  const [rolePermissionDraft, setRolePermissionDraft] = useState([]);
  const [pendingDeleteCatalogExerciseId, setPendingDeleteCatalogExerciseId] =
    useState(null);
  const [pendingDeleteWorkoutSessionId, setPendingDeleteWorkoutSessionId] =
    useState(null);
  const [
    pendingDeleteCurrentWorkoutExerciseId,
    setPendingDeleteCurrentWorkoutExerciseId,
  ] = useState(null);
  /** id упражнения → свёрнут блок подходов в модалке активной тренировки */
  const [
    activeWorkoutCollapsedExerciseIds,
    setActiveWorkoutCollapsedExerciseIds,
  ] = useState({});
  const [selectedWorkoutHistorySession, setSelectedWorkoutHistorySession] =
    useState(null);
  const [historyDatePeriodPreset, setHistoryDatePeriodPreset] = useState('7');
  const [historyDateFrom, setHistoryDateFrom] = useState(
    () => getRollingWeekRange().from,
  );
  const [historyDateTo, setHistoryDateTo] = useState(
    () => getRollingWeekRange().to,
  );
  const [historyWorkoutLogs, setHistoryWorkoutLogs] = useState([]);
  const [workoutHistoryForStats, setWorkoutHistoryForStats] = useState([]);
  const [bikeActivities, setBikeActivities] = useState([]);
  const [bikeDatePeriodPreset, setBikeDatePeriodPreset] = useState('7');
  const [bikeDateFrom, setBikeDateFrom] = useState(
    () => getRollingWeekRange().from,
  );
  const [bikeDateTo, setBikeDateTo] = useState(() => getRollingWeekRange().to);
  const [bikeImportMessage, setBikeImportMessage] = useState('');
  const [bikeDetail, setBikeDetail] = useState(null);
  const [bikeDetailLoading, setBikeDetailLoading] = useState(false);
  const [bikeDetailOpen, setBikeDetailOpen] = useState(false);
  const [pendingDeleteBikeActivityId, setPendingDeleteBikeActivityId] =
    useState(null);
  const [investments, setInvestments] = useState([]);
  const [investmentSummary, setInvestmentSummary] = useState(null);
  const [investmentsMessage, setInvestmentsMessage] = useState('');
  const [isCreateInvestmentModalOpen, setIsCreateInvestmentModalOpen] =
    useState(false);
  const [investmentDraft, setInvestmentDraft] = useState({
    ticker: '',
    name: '',
    assetType: 'stock',
    quantity: '',
    averagePrice: '',
    currency: 'RUB',
    notes: '',
  });
  const planningImportInputRef = useRef(null);
  const exercisesImportInputRef = useRef(null);
  const bikeTcxImportRef = useRef(null);
  const isApiLoading = activeApiRequests > 0;

  async function request(method, path, body, token) {
    setActiveApiRequests((prev) => prev + 1);
    try {
      const response = await fetch(baseUrl.trim().replace(/\/+$/, '') + path, {
        method,
        headers: {
          ...(body ? { 'Content-Type': 'application/json' } : {}),
          ...(token ? { Authorization: 'Bearer ' + token } : {}),
        },
        ...(body ? { body: JSON.stringify(body) } : {}),
      });
      const data = await response.json().catch(() => ({}));
      return { status: response.status, ok: response.ok, data };
    } catch (error) {
      return {
        status: 0,
        ok: false,
        data: { message: 'Ошибка сети', detail: String(error) },
      };
    } finally {
      setActiveApiRequests((prev) => Math.max(0, prev - 1));
    }
  }

  function handleResult(result) {
    setErrorView(
      result.ok
        ? 'Ошибок нет.'
        : JSON.stringify(
            {
              status: result.status,
              message: result.data?.message ?? 'Запрос завершился с ошибкой',
              detail: result.data,
            },
            null,
            2,
          ),
    );
  }

  function profileToNumberOrNull(value) {
    const raw = String(value ?? '').trim();
    if (!raw) return null;
    const parsed = Number(raw);
    return Number.isNaN(parsed) ? null : parsed;
  }

  function ageFromBirthDate(isoDateStr) {
    if (!isoDateStr || !String(isoDateStr).trim()) return '';
    const parts = String(isoDateStr).slice(0, 10).split('-');
    if (parts.length !== 3) return '';
    const y = Number(parts[0]);
    const m = Number(parts[1]) - 1;
    const day = Number(parts[2]);
    if (!Number.isFinite(y) || !Number.isFinite(m) || !Number.isFinite(day))
      return '';
    const d = new Date(y, m, day);
    if (Number.isNaN(d.getTime())) return '';
    const today = new Date();
    let age = today.getFullYear() - d.getFullYear();
    const md = today.getMonth() - d.getMonth();
    if (md < 0 || (md === 0 && today.getDate() < d.getDate())) age -= 1;
    return age >= 0 && age < 150 ? String(age) : '';
  }

  function birthDateIsoFromAgeYearsString(ageStr) {
    const n = parseInt(String(ageStr).trim(), 10);
    if (!Number.isFinite(n) || n < 0 || n > 150) return '';
    const today = new Date();
    const birth = new Date(
      today.getFullYear() - n,
      today.getMonth(),
      today.getDate(),
    );
    return birth.toISOString().slice(0, 10);
  }

  /** Вес, рост и возраст: приоритет у данных профиля, если они заданы; иначе сохранённые доп. значения. */
  function mergeAiExtrasWithProfile(merged, keySet) {
    const next = { ...merged };
    if (keySet.has('weight')) {
      const p = profileWeightKg.trim();
      const s = String(next.weight ?? '').trim();
      next.weight = p || s || '';
    }
    if (keySet.has('height')) {
      const p = profileHeightCm.trim();
      const s = String(next.height ?? '').trim();
      next.height = p || s || '';
    }
    if (keySet.has('age')) {
      const fromBirth = ageFromBirthDate(profileBirthDate);
      const s = String(next.age ?? '').trim();
      next.age = fromBirth || s || '';
    }
    return next;
  }

  function tryGetUserNameFromToken(token) {
    try {
      const payloadPart = token.split('.')[1];
      if (!payloadPart) return '';
      const normalized = payloadPart.replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized + '='.repeat((4 - (normalized.length % 4)) % 4);
      const payload = JSON.parse(window.atob(padded));
      return payload.unique_name || payload.name || payload.sub || '';
    } catch {
      return '';
    }
  }

  function tryGetRolesFromToken(token) {
    try {
      const payloadPart = token.split('.')[1];
      if (!payloadPart) return [];
      const normalized = payloadPart.replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized + '='.repeat((4 - (normalized.length % 4)) % 4);
      const payload = JSON.parse(window.atob(padded));
      const rawRole =
        payload.role ||
        payload.roles ||
        payload[
          'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
        ] ||
        '';
      if (Array.isArray(rawRole)) {
        return rawRole.map((role) => String(role)).filter(Boolean);
      }
      if (rawRole) return [String(rawRole)];
      return [];
    } catch {
      return [];
    }
  }

  function tryGetPermissionsFromToken(token) {
    try {
      const payloadPart = token.split('.')[1];
      if (!payloadPart) return [];
      const normalized = payloadPart.replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized + '='.repeat((4 - (normalized.length % 4)) % 4);
      const payload = JSON.parse(window.atob(padded));
      const raw =
        payload.permission ||
        payload.permissions ||
        payload[
          'http://schemas.microsoft.com/ws/2008/06/identity/claims/permission'
        ] ||
        '';
      if (Array.isArray(raw)) {
        return raw.map((item) => String(item)).filter(Boolean);
      }
      if (raw) return [String(raw)];
      return [];
    } catch {
      return [];
    }
  }

  async function loadDialogs(token = aiToken, forceDialogId = '') {
    if (!token) return;
    const result = await request('GET', '/ai/dialogs', null, token);
    handleResult(result);
    if (!result.ok) return;
    const incoming = Array.isArray(result.data) ? result.data : [];
    setDialogs(incoming);
    const nextDialogId =
      forceDialogId ||
      (incoming.some((d) => d.id === currentDialogId)
        ? currentDialogId
        : incoming[0]?.id || '');
    setCurrentDialogId(nextDialogId);
    if (nextDialogId) {
      await loadDialogMessages(nextDialogId, token);
    } else {
      setChatMessages([
        { role: 'assistant', content: 'Создай диалог, чтобы начать чат.' },
      ]);
    }
  }

  async function loadDialogMessages(dialogId, token = aiToken) {
    if (!dialogId) return;
    const result = await request(
      'GET',
      `/ai/dialogs/${dialogId}/messages`,
      null,
      token,
    );
    handleResult(result);
    if (!result.ok) return;
    const messages = Array.isArray(result.data) ? result.data : [];
    setChatMessages(
      messages.length
        ? messages
        : [
            {
              role: 'assistant',
              content: 'Пустой диалог. Отправь первое сообщение.',
            },
          ],
    );
  }

  async function loginAndStore(navigate) {
    if (isLoginLoading) return;
    setIsLoginLoading(true);
    setLoginError('');
    try {
      const result = await request('POST', '/auth/login', {
        username: username.trim(),
        password,
      });
      handleResult(result);
      if (!result.ok) {
        const message =
          result.data?.message ||
          (result.status === 401 ? 'Неверный логин или пароль.' : '') ||
          (result.status === 0
            ? 'Сервер недоступен. Проверь подключение и адрес API.'
            : '') ||
          'Не удалось выполнить вход. Попробуй еще раз.';
        setLoginError(message);
        return;
      }
      const token = result.data?.accessToken || '';
      setAccessToken(token);
      setAdminToken(token);
      setAiToken(token);
      const nextRoles = tryGetRolesFromToken(token);
      const nextPermissions = Array.isArray(result.data?.permissions)
        ? result.data.permissions.map((item) => String(item))
        : tryGetPermissionsFromToken(token);
      setCurrentUserName(tryGetUserNameFromToken(token) || username.trim());
      setCurrentUserRoles(nextRoles);
      setCurrentUserPermissions(nextPermissions);
      const defaultTab =
        [
          { id: 'workouts', permission: APP_PERMISSION.Workouts },
          { id: 'bike', permission: APP_PERMISSION.Bike },
          { id: 'progress', permission: APP_PERMISSION.Progress },
          { id: 'habits', permission: APP_PERMISSION.Habits },
          { id: 'todos', permission: APP_PERMISSION.Todos },
          { id: 'investments', permission: APP_PERMISSION.Investments },
          { id: 'profile', permission: APP_PERMISSION.Profile },
          { id: 'ai', permission: APP_PERMISSION.AiAssistant },
          { id: 'admin', permission: null },
        ].find((item) => {
          if (item.id === 'admin') {
            return nextPermissions.some((perm) =>
              String(perm).startsWith('admin.'),
            );
          }
          return nextPermissions.some(
            (perm) =>
              String(perm).toLowerCase() ===
              String(item.permission).toLowerCase(),
          );
        })?.id || 'profile';
      setTab(defaultTab);
      if (
        nextPermissions.some(
          (item) =>
            String(item).toLowerCase() ===
            APP_PERMISSION.AiAssistant.toLowerCase(),
        )
      ) {
        await loadDialogs(token);
        await loadAiAssistants(token);
      }
      await loadMyProfile(token);
      navigate('/app');
    } finally {
      setIsLoginLoading(false);
    }
  }

  function formatReviewDateUtc(iso) {
    if (!iso) return '—';
    try {
      return new Date(iso).toLocaleString('ru-RU', {
        dateStyle: 'short',
        timeStyle: 'short',
      });
    } catch {
      return String(iso);
    }
  }

  async function loadWeeklyTrainingReviews(token = aiToken) {
    if (!token) return;
    const result = await request(
      'GET',
      '/ai/trainer/weekly-reviews',
      null,
      token,
    );
    handleResult(result);
    if (result.ok) {
      setWeeklyTrainingReviews(
        Array.isArray(result.data?.reviews) ? result.data.reviews : [],
      );
    }
  }

  async function openWeeklyReviewModal(reviewId) {
    if (!aiToken) return;
    setIsWeeklyReviewModalOpen(true);
    setWeeklyReviewModalLoading(true);
    setSelectedWeeklyReview(null);
    const result = await request(
      'GET',
      `/ai/trainer/weekly-reviews/${reviewId}`,
      null,
      aiToken,
    );
    setWeeklyReviewModalLoading(false);
    handleResult(result);
    if (result.ok) {
      setSelectedWeeklyReview(result.data);
    } else {
      setIsWeeklyReviewModalOpen(false);
    }
  }

  function closeWeeklyReviewModal() {
    setIsWeeklyReviewModalOpen(false);
    setSelectedWeeklyReview(null);
    setWeeklyReviewModalLoading(false);
  }

  function openDeleteWeeklyReviewModal(review) {
    if (!review?.id) return;
    setPendingDeleteWeeklyReview({
      id: review.id,
      periodFrom: review.periodFrom,
      periodTo: review.periodTo,
    });
  }

  async function confirmDeleteWeeklyReview() {
    const reviewId = pendingDeleteWeeklyReview?.id;
    if (!aiToken || !reviewId) return;

    const result = await request(
      'DELETE',
      `/ai/trainer/weekly-reviews/${reviewId}`,
      null,
      aiToken,
    );
    handleResult(result);
    if (!result.ok) return;

    setPendingDeleteWeeklyReview(null);
    if (selectedWeeklyReview?.id === reviewId) {
      closeWeeklyReviewModal();
    }
    setWeeklyTrainingReviews((prev) =>
      prev.filter((x) => x.id !== reviewId),
    );
    await loadWeeklyTrainingReviews(aiToken);
  }

  function resolveAssistantIdForTrainerChat() {
    const previewMatch =
      chatAssistantPreviewId &&
      !(tab === 'workouts' && workoutsSubTab === 'ai-trainer')
        ? (aiAssistants.find(
            (x) => String(x.id) === String(chatAssistantPreviewId),
          ) ??
          adminAiAssistants.find(
            (x) => String(x.id) === String(chatAssistantPreviewId),
          ))
        : null;
    const trainerAssistant = aiAssistants.find(
      (x) => x.assistantCode === 'trainer',
    );
    const selectedAssistant = aiAssistants.find((x) => x.selected);
    const useWorkoutTrainerChat =
      tab === 'workouts' && workoutsSubTab === 'ai-trainer';
    return (
      previewMatch?.id ??
      (useWorkoutTrainerChat && trainerAssistant
        ? trainerAssistant.id
        : null) ??
      (selectedAssistant?.assistantCode === 'trainer'
        ? selectedAssistant.id
        : null) ??
      trainerAssistant?.id ??
      null
    );
  }

  async function requestPreviousWeekWeeklyReview() {
    if (!aiToken)
      return setErrorView(
        'Сначала войдите в систему или вставьте JWT-токен в поле AI-токена.',
      );
    if (weeklyReviewGenerating) return;

    const { endingOn } = getPreviousCalendarWeekUtc();
    const trainerAssistant = aiAssistants.find(
      (x) => x.assistantCode === 'trainer',
    );

    setWeeklyReviewGenerating(true);
    const result = await request(
      'POST',
      '/ai/trainer/weekly-review',
      {
        days: 7,
        endingOn,
        writeToDialog: false,
        assistantId: trainerAssistant?.id ?? null,
      },
      aiToken,
    );
    setWeeklyReviewGenerating(false);
    handleResult(result);
    if (!result.ok) return;

    await loadWeeklyTrainingReviews(aiToken);
    if (result.data?.generated) {
      await loadMyProfile(aiToken);
    }
    const reviewId = result.data?.reviewId;
    if (reviewId) {
      await openWeeklyReviewModal(reviewId);
    }
  }

  async function sendChat(overridePrompt) {
    const prompt = (overridePrompt ?? chatPrompt).trim();
    if (!prompt) return setErrorView('Введите сообщение для AI помощника.');
    if (!aiToken)
      return setErrorView(
        'Сначала войдите в систему или вставьте JWT-токен в поле AI-токена.',
      );

    setChatMessages((prev) => [...prev, { role: 'user', content: prompt }]);
    setChatPrompt('');

    const previewMatch =
      chatAssistantPreviewId &&
      !(tab === 'workouts' && workoutsSubTab === 'ai-trainer')
        ? (aiAssistants.find(
            (x) => String(x.id) === String(chatAssistantPreviewId),
          ) ??
          adminAiAssistants.find(
            (x) => String(x.id) === String(chatAssistantPreviewId),
          ))
        : null;
    const trainerAssistant = aiAssistants.find(
      (x) => x.assistantCode === 'trainer',
    );
    const selectedAssistant = aiAssistants.find((x) => x.selected);
    const useWorkoutTrainerChat =
      tab === 'workouts' && workoutsSubTab === 'ai-trainer';
    const assistantIdForChat =
      previewMatch?.id ??
      (useWorkoutTrainerChat && trainerAssistant
        ? trainerAssistant.id
        : null) ??
      selectedAssistant?.id ??
      null;
    const result = await request(
      'POST',
      '/ai/chat',
      {
        prompt,
        dialogId: currentDialogId || null,
        assistantId: assistantIdForChat ?? null,
      },
      aiToken,
    );
    handleResult(result);
    if (!result.ok) {
      const errText =
        result.data?.message ||
        (result.status === 502
          ? 'Сервис ИИ недоступен (502). Проверьте OPENAI_API_KEY, модель и что API запущен.'
          : `Ошибка запроса (${result.status}).`);
      setChatMessages((prev) => [
        ...prev,
        { role: 'assistant', content: errText },
      ]);
      return;
    }

    const dialogId = result.data?.dialogId || currentDialogId;
    setCurrentDialogId(dialogId);
    setChatMessages((prev) => [
      ...prev,
      {
        role: 'assistant',
        content: result.data?.response || 'Нет текста ответа.',
      },
    ]);
    await loadDialogs(aiToken, dialogId);
    await loadMyProfile(aiToken);
  }

  async function submitAiNewDialog() {
    const title = (aiDialogTitleDraft || '').trim() || 'Новый диалог';
    const r = await request('POST', '/ai/dialogs', { title }, aiToken);
    handleResult(r);
    if (r.ok) {
      setAiDialogModalKind(null);
      setAiDialogTitleDraft('');
      await loadDialogs(aiToken, r.data?.id);
    }
  }

  async function submitAiRenameDialog() {
    if (!currentDialogId) return setErrorView('Нет выбранного диалога.');
    const title = (aiDialogTitleDraft || '').trim();
    if (!title) return setErrorView('Введи название.');
    const r = await request(
      'PUT',
      `/ai/dialogs/${currentDialogId}`,
      { title },
      aiToken,
    );
    handleResult(r);
    if (r.ok) {
      setAiDialogModalKind(null);
      setAiDialogTitleDraft('');
      await loadDialogs(aiToken, currentDialogId);
    }
  }

  async function submitAiDeleteDialog() {
    if (!currentDialogId) return;
    const r = await request(
      'DELETE',
      `/ai/dialogs/${currentDialogId}`,
      null,
      aiToken,
    );
    handleResult(r);
    if (r.ok) {
      setAiDialogModalKind(null);
      await loadDialogs(aiToken);
    }
  }

  async function loadAiAssistants(token = aiToken) {
    if (!token) return;
    const result = await request('GET', '/ai/assistants', null, token);
    handleResult(result);
    if (!result.ok) return;
    const list = Array.isArray(result.data?.assistants)
      ? result.data.assistants
      : [];
    setAiAssistants(list);
  }

  async function selectAiAssistant(assistantId) {
    const r = await request(
      'PUT',
      '/ai/assistants/selection',
      { assistantId },
      aiToken,
    );
    handleResult(r);
    if (r.ok) await loadAiAssistants(aiToken);
  }

  async function toggleAiAssistantRow(id, selected) {
    if (selected) {
      await selectAiAssistant(null);
    } else {
      await selectAiAssistant(id);
    }
  }

  async function loadAdminAiAssistants() {
    const result = await request(
      'GET',
      '/admin/ai-assistants',
      null,
      adminToken,
    );
    handleResult(result);
    if (!result.ok) return;
    setAdminAiAssistants(Array.isArray(result.data) ? result.data : []);
  }

  function openAssistantCreateModal() {
    setAdminExtraFieldsList([]);
    setAssistantDraft({
      id: '',
      name: '',
      description: '',
      systemPrompt: '',
      settingsJson: '',
      sortOrder: adminAiAssistants.length
        ? Math.max(...adminAiAssistants.map((x) => x.sortOrder || 0)) + 1
        : 0,
      isActive: true,
    });
    setAssistantModalKind('create');
  }

  function openAssistantEditModal(row) {
    setAssistantDraft({
      id: row.id,
      name: row.name || '',
      description: row.description || '',
      systemPrompt: row.systemPrompt || '',
      settingsJson: row.settingsJson || '',
      sortOrder: row.sortOrder ?? 0,
      isActive: Boolean(row.isActive),
    });
    setAssistantModalKind('edit');
    void loadAdminExtraFields(row.id);
  }

  async function submitAssistantModal() {
    const name = (assistantDraft.name || '').trim();
    const systemPrompt = (assistantDraft.systemPrompt || '').trim();
    if (!name) return setErrorView('Укажи название помощника.');
    if (!systemPrompt) return setErrorView('Укажи системный промпт.');

    const body = {
      name,
      description: (assistantDraft.description || '').trim() || null,
      systemPrompt,
      settingsJson: (assistantDraft.settingsJson || '').trim() || null,
      sortOrder: Number(assistantDraft.sortOrder) || 0,
      isActive: Boolean(assistantDraft.isActive),
    };

    const modeBeforeSave = assistantModalKind;
    let r;
    if (assistantModalKind === 'create') {
      r = await request('POST', '/admin/ai-assistants', body, adminToken);
    } else if (assistantModalKind === 'edit' && assistantDraft.id) {
      r = await request(
        'PUT',
        `/admin/ai-assistants/${assistantDraft.id}`,
        body,
        adminToken,
      );
    } else {
      return;
    }
    handleResult(r);
    if (!r.ok) return;

    await loadAdminAiAssistants();
    if (hasAiAccess) await loadAiAssistants(aiToken);

    if (modeBeforeSave === 'create' && r.data?.id) {
      setAssistantDraft((d) => ({ ...d, id: r.data.id }));
      setAssistantModalKind('edit');
      await loadAdminExtraFields(r.data.id);
    } else if (assistantDraft.id) {
      await loadAdminExtraFields(assistantDraft.id);
    }
  }

  async function confirmDeleteAssistant() {
    if (!pendingDeleteAssistantId) return;
    const r = await request(
      'DELETE',
      `/admin/ai-assistants/${pendingDeleteAssistantId}`,
      null,
      adminToken,
    );
    handleResult(r);
    if (r.ok) {
      setPendingDeleteAssistantId(null);
      await loadAdminAiAssistants();
      if (hasAiAccess) await loadAiAssistants(aiToken);
    }
  }

  async function openAiExtraInfoModal(assistantId) {
    if (!assistantId) return;
    setAiExtraInfoAssistantId(assistantId);
    setAiExtraInfoDefinitions([]);
    setAiExtraInfoValues({});
    setIsAiExtraInfoModalOpen(true);
    const r = await request(
      'GET',
      `/ai/assistant-extra-fields?assistantId=${encodeURIComponent(assistantId)}`,
      null,
      aiToken,
    );
    handleResult(r);
    if (!r.ok) return;
    const defs = Array.isArray(r.data?.definitions) ? r.data.definitions : [];
    const vals =
      r.data?.values && typeof r.data.values === 'object' ? r.data.values : {};
    setAiExtraInfoDefinitions(defs);
    const merged = {};
    for (const d of defs) {
      merged[d.fieldKey] = vals[d.fieldKey] ?? '';
    }
    const keySet = new Set(defs.map((d) => d.fieldKey));
    setAiExtraInfoValues(mergeAiExtrasWithProfile(merged, keySet));
  }

  function handleAiExtraFieldChange(fieldKey, value) {
    setAiExtraInfoValues((prev) => ({ ...prev, [fieldKey]: value }));
    if (fieldKey === 'weight') setProfileWeightKg(value);
    if (fieldKey === 'height') setProfileHeightCm(value);
    if (fieldKey === 'age') {
      const t = String(value).trim();
      if (!t) {
        setProfileBirthDate('');
        return;
      }
      const iso = birthDateIsoFromAgeYearsString(t);
      if (iso) setProfileBirthDate(iso);
    }
  }

  async function syncLinkedAiExtrasFromProfileBody(profileBody) {
    if (!accessToken || !aiToken || !profileBody) return;
    const weight =
      profileBody.weightKg != null && profileBody.weightKg !== undefined
        ? String(profileBody.weightKg)
        : '';
    const height =
      profileBody.heightCm != null && profileBody.heightCm !== undefined
        ? String(profileBody.heightCm)
        : '';
    const age = ageFromBirthDate(profileBody.birthDate || '');
    for (const a of aiAssistants) {
      const ar = await request(
        'GET',
        `/ai/assistant-extra-fields?assistantId=${encodeURIComponent(a.id)}`,
        null,
        aiToken,
      );
      if (!ar.ok) continue;
      const defs = Array.isArray(ar.data?.definitions)
        ? ar.data.definitions
        : [];
      const keySet = new Set(defs.map((d) => d.fieldKey));
      const vals =
        ar.data?.values && typeof ar.data.values === 'object'
          ? { ...ar.data.values }
          : {};
      let touched = false;
      if (keySet.has('weight')) {
        vals.weight = weight;
        touched = true;
      }
      if (keySet.has('height')) {
        vals.height = height;
        touched = true;
      }
      if (keySet.has('age')) {
        vals.age = age;
        touched = true;
      }
      if (!touched) continue;
      await request(
        'PUT',
        '/ai/assistant-extra-fields',
        { assistantId: a.id, values: vals },
        aiToken,
      );
    }
  }

  async function submitAiExtraInfoModal() {
    if (!aiExtraInfoAssistantId) return;
    const r = await request(
      'PUT',
      '/ai/assistant-extra-fields',
      { assistantId: aiExtraInfoAssistantId, values: aiExtraInfoValues },
      aiToken,
    );
    handleResult(r);
    if (!r.ok) return;

    const keys = new Set(aiExtraInfoDefinitions.map((d) => d.fieldKey));
    const v = aiExtraInfoValues;
    let birthDate = profileBirthDate;
    let heightCm = profileHeightCm;
    let weightKg = profileWeightKg;
    if (keys.has('weight'))
      weightKg = v.weight != null ? String(v.weight) : weightKg;
    if (keys.has('height'))
      heightCm = v.height != null ? String(v.height) : heightCm;
    if (keys.has('age')) {
      const a = v.age != null ? String(v.age).trim() : '';
      if (a) {
        const iso = birthDateIsoFromAgeYearsString(a);
        if (iso) birthDate = iso;
      }
    }

    if (accessToken) {
      const body = {
        birthDate: birthDate || null,
        heightCm: profileToNumberOrNull(heightCm),
        weightKg: profileToNumberOrNull(weightKg),
        phone: profilePhone.trim() || null,
        city: profileCity.trim() || null,
        about: profileAbout.trim() || null,
        firstName: profileFirstName.trim() || null,
        lastName: profileLastName.trim() || null,
      };
      const pr = await request('PUT', '/users/me', body, accessToken);
      handleResult(pr);
      if (!pr.ok) return;
      await loadMyProfile(accessToken);
      await syncLinkedAiExtrasFromProfileBody(body);
    }

    setIsAiExtraInfoModalOpen(false);
  }

  async function loadAdminExtraFields(assistantId) {
    const r = await request(
      'GET',
      `/admin/ai-assistants/${assistantId}/extra-fields`,
      null,
      adminToken,
    );
    handleResult(r);
    if (!r.ok) return;
    setAdminExtraFieldsList(Array.isArray(r.data) ? r.data : []);
  }

  function openAdminExtraFieldCreate() {
    setAdminExtraFieldDraft({
      id: '',
      fieldKey: '',
      label: '',
      fieldType: 'text',
      sortOrder: adminExtraFieldsList.length
        ? Math.max(...adminExtraFieldsList.map((x) => x.sortOrder || 0)) + 1
        : 0,
      isRequired: false,
    });
    setAdminExtraFieldModalKind('create');
  }

  function openAdminExtraFieldEdit(row) {
    setAdminExtraFieldDraft({
      id: row.id,
      fieldKey: row.fieldKey || '',
      label: row.label || '',
      fieldType: row.fieldType || 'text',
      sortOrder: row.sortOrder ?? 0,
      isRequired: Boolean(row.isRequired),
    });
    setAdminExtraFieldModalKind('edit');
  }

  async function submitAdminExtraFieldModal() {
    const aid = assistantDraft.id;
    if (!aid) return setErrorView('Сначала сохрани помощника.');
    const label = (adminExtraFieldDraft.label || '').trim();
    const rawKey = (adminExtraFieldDraft.fieldKey || '')
      .trim()
      .toLowerCase()
      .replace(/\s+/g, '_');
    if (!rawKey || !label)
      return setErrorView('Укажи ключ поля (латиница, цифры, _) и подпись.');
    const body = {
      fieldKey: rawKey,
      label,
      fieldType: adminExtraFieldDraft.fieldType || 'text',
      sortOrder: Number(adminExtraFieldDraft.sortOrder) || 0,
      isRequired: Boolean(adminExtraFieldDraft.isRequired),
    };
    let r;
    if (adminExtraFieldModalKind === 'create') {
      r = await request(
        'POST',
        `/admin/ai-assistants/${aid}/extra-fields`,
        body,
        adminToken,
      );
    } else if (adminExtraFieldModalKind === 'edit' && adminExtraFieldDraft.id) {
      r = await request(
        'PUT',
        `/admin/ai-assistants/${aid}/extra-fields/${adminExtraFieldDraft.id}`,
        body,
        adminToken,
      );
    } else {
      return;
    }
    handleResult(r);
    if (r.ok) {
      setAdminExtraFieldModalKind(null);
      await loadAdminExtraFields(aid);
    }
  }

  async function confirmDeleteExtraField() {
    if (!pendingDeleteExtraField) return;
    const { assistantId, fieldId } = pendingDeleteExtraField;
    const r = await request(
      'DELETE',
      `/admin/ai-assistants/${assistantId}/extra-fields/${fieldId}`,
      null,
      adminToken,
    );
    handleResult(r);
    if (r.ok) {
      setPendingDeleteExtraField(null);
      await loadAdminExtraFields(assistantId);
    }
  }

  async function loadAdminCategories() {
    if (!adminToken) return;
    const result = await request('GET', '/admin/categories', null, adminToken);
    handleResult(result);
    if (!result.ok) return;
    setAdminCategories(Array.isArray(result.data) ? result.data : []);
  }

  function openAdminCategoryCreateModal() {
    const nextSort = adminCategories.length
      ? Math.max(...adminCategories.map((x) => x.sortOrder || 0)) + 1
      : 1;
    setAdminCategoryDraft({
      id: '',
      name: '',
      description: '',
      isActive: true,
      sortOrder: nextSort,
    });
    setAdminCategoryModalKind('create');
  }

  function openAdminCategoryEditModal(category) {
    if (!category) return;
    setAdminCategoryDraft({
      id: category.id,
      name: category.name || '',
      description: category.description || '',
      isActive: Boolean(category.isActive),
      sortOrder: Number(category.sortOrder) || 0,
    });
    setAdminCategoryModalKind('edit');
  }

  async function submitAdminCategoryModal() {
    const name = String(adminCategoryDraft.name ?? '').trim();
    if (!name) return setErrorView('Укажи название категории.');
    const body = {
      name,
      description: String(adminCategoryDraft.description ?? '').trim() || null,
      isActive: Boolean(adminCategoryDraft.isActive),
      sortOrder: Number(adminCategoryDraft.sortOrder) || 0,
    };
    let r;
    if (adminCategoryModalKind === 'create') {
      r = await request('POST', '/admin/categories', body, adminToken);
    } else if (adminCategoryModalKind === 'edit' && adminCategoryDraft.id) {
      r = await request(
        'PUT',
        `/admin/categories/${adminCategoryDraft.id}`,
        body,
        adminToken,
      );
    } else {
      return;
    }
    handleResult(r);
    if (r.ok) {
      setAdminCategoryModalKind(null);
      await loadAdminCategories();
      await loadUserCategories(accessToken);
    }
  }

  async function confirmDeleteCategory() {
    if (!pendingDeleteCategory?.id) return;
    const r = await request(
      'DELETE',
      `/admin/categories/${pendingDeleteCategory.id}`,
      null,
      adminToken,
    );
    handleResult(r);
    if (r.ok) {
      setPendingDeleteCategory(null);
      await loadAdminCategories();
      await loadUserCategories(accessToken);
    }
  }

  async function loadAdminRoles() {
    if (!adminToken) return;
    const result = await request('GET', '/admin/roles', null, adminToken);
    handleResult(result);
    if (!result.ok) return;
    setAdminRoles(Array.isArray(result.data) ? result.data : []);
  }

  function openAdminRoleCreateModal() {
    const nextSort = adminRoles.length
      ? Math.max(...adminRoles.map((x) => x.sortOrder || 0)) + 1
      : 1;
    setAdminRoleDraft({
      id: '',
      name: '',
      label: '',
      description: '',
      isActive: true,
      sortOrder: nextSort,
      isSystem: false,
    });
    setAdminRoleModalKind('create');
  }

  function openAdminRoleEditModal(role) {
    if (!role) return;
    setAdminRoleDraft({
      id: role.id,
      name: role.name || '',
      label: role.label || '',
      description: role.description || '',
      isActive: Boolean(role.isActive),
      sortOrder: Number(role.sortOrder) || 0,
      isSystem: Boolean(role.isSystem),
    });
    setAdminRoleModalKind('edit');
  }

  async function submitAdminRoleModal() {
    const label = String(adminRoleDraft.label ?? '').trim();
    if (!label) return setErrorView('Укажи название роли.');
    const body = {
      label,
      description: String(adminRoleDraft.description ?? '').trim() || null,
      isActive: Boolean(adminRoleDraft.isActive),
      sortOrder: Number(adminRoleDraft.sortOrder) || 0,
    };
    let r;
    if (adminRoleModalKind === 'create') {
      const name = String(adminRoleDraft.name ?? '').trim();
      if (!name) return setErrorView('Укажи код роли (латиница, например Moderator).');
      r = await request(
        'POST',
        '/admin/roles',
        { ...body, name },
        adminToken,
      );
    } else if (adminRoleModalKind === 'edit' && adminRoleDraft.id) {
      r = await request(
        'PUT',
        `/admin/roles/${adminRoleDraft.id}`,
        body,
        adminToken,
      );
    } else {
      return;
    }
    handleResult(r);
    if (r.ok) {
      setAdminRoleModalKind(null);
      await loadAdminRoles();
    }
  }

  async function confirmDeleteRole() {
    if (!pendingDeleteRole?.id) return;
    const r = await request(
      'DELETE',
      `/admin/roles/${pendingDeleteRole.id}`,
      null,
      adminToken,
    );
    handleResult(r);
    if (r.ok) {
      setPendingDeleteRole(null);
      await loadAdminRoles();
    }
  }

  async function loadAdminPermissionsCatalog() {
    if (!adminToken) return;
    const result = await request(
      'GET',
      '/admin/roles/permissions/catalog',
      null,
      adminToken,
    );
    handleResult(result);
    if (!result.ok) return;
    setAdminPermissionsCatalog(Array.isArray(result.data) ? result.data : []);
  }

  async function openRolePermissionsModal(role) {
    if (!role?.id) return;
    setRolePermissionsModalRole(role);
    const result = await request(
      'GET',
      `/admin/roles/${role.id}/permissions`,
      null,
      adminToken,
    );
    handleResult(result);
    if (!result.ok) {
      setRolePermissionsModalRole(null);
      return;
    }
    const codes = Array.isArray(result.data?.permissionCodes)
      ? result.data.permissionCodes.map((item) => String(item))
      : [];
    setRolePermissionDraft(codes);
  }

  function toggleRolePermissionDraft(code, checked) {
    setRolePermissionDraft((prev) => {
      if (checked) {
        return prev.includes(code) ? prev : [...prev, code];
      }
      return prev.filter((item) => item !== code);
    });
  }

  async function submitRolePermissionsModal() {
    if (!rolePermissionsModalRole?.id) return;
    const result = await request(
      'PUT',
      `/admin/roles/${rolePermissionsModalRole.id}/permissions`,
      { permissionCodes: rolePermissionDraft },
      adminToken,
    );
    handleResult(result);
    if (!result.ok) return;
    setRolePermissionsModalRole(null);
    setRolePermissionDraft([]);
  }

  async function loadUsers() {
    const result = await request('GET', '/admin/users', null, adminToken);
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
    const result = await request('GET', '/users/me', null, token);
    handleResult(result);
    if (!result.ok) return;

    const profile = result.data || {};
    setProfileBirthDate(profile.birthDate || '');
    setProfileHeightCm(
      profile.heightCm === null || profile.heightCm === undefined
        ? ''
        : String(profile.heightCm),
    );
    setProfileWeightKg(
      profile.weightKg === null || profile.weightKg === undefined
        ? ''
        : String(profile.weightKg),
    );
    setProfilePhone(profile.phone || '');
    setProfileCity(profile.city || '');
    setProfileAbout(profile.about || '');
    setProfileFirstName(profile.firstName || '');
    setProfileLastName(profile.lastName || '');
    setProfileAiSummary(profile.aiSummary || '');
    setProfileTelegramLinked(Boolean(profile.telegramLinked));
    if (Array.isArray(profile.permissions)) {
      setCurrentUserPermissions(profile.permissions.map((item) => String(item)));
    }
    await loadUserCategories(token);
    const perms = Array.isArray(profile.permissions)
      ? profile.permissions.map((item) => String(item))
      : currentUserPermissions;
    const profileCan = (code) =>
      perms.some(
        (item) => String(item).toLowerCase() === String(code).toLowerCase(),
      );
    if (profileCan(APP_PERMISSION.Progress)) await loadWeightTracker(token);
    if (profileCan(APP_PERMISSION.Habits)) await loadHabitsOverview(token);
    if (profileCan(APP_PERMISSION.Todos)) await loadTodos(token);
  }

  async function loadUserCategories(token = accessToken) {
    if (!token) return;
    const result = await request('GET', '/users/me/categories', null, token);
    handleResult(result);
    if (!result.ok) return;
    setUserCategories(Array.isArray(result.data) ? result.data : []);
  }

  function openTelegramLinkModal() {
    setTelegramLinkError('');
    setTelegramLinkUrl('');
    setTelegramLinkExpiresAt('');
    setIsTelegramLinkModalOpen(true);
  }

  async function createTelegramDeepLink() {
    if (!accessToken) return;
    setTelegramLinkLoading(true);
    setTelegramLinkError('');
    setTelegramLinkUrl('');
    setTelegramLinkExpiresAt('');
    const result = await request(
      'POST',
      '/users/me/telegram/link',
      null,
      accessToken,
    );
    handleResult(result);
    setTelegramLinkLoading(false);
    if (!result.ok) {
      setTelegramLinkError(
        result.data?.message || 'Не удалось создать ссылку.',
      );
      return;
    }
    const d = result.data || {};
    if (d.deepLinkUrl) setTelegramLinkUrl(d.deepLinkUrl);
    if (d.expiresAtUtc) setTelegramLinkExpiresAt(d.expiresAtUtc);
  }

  async function unlinkTelegramAccount() {
    if (!accessToken) return;
    const result = await request(
      'DELETE',
      '/users/me/telegram',
      null,
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;
    setIsTelegramUnlinkConfirmOpen(false);
    setIsTelegramLinkModalOpen(false);
    setTelegramLinkUrl('');
    setTelegramLinkExpiresAt('');
    await loadMyProfile(accessToken);
  }

  async function loadWeightTracker(token = accessToken) {
    if (!token) return;
    const result = await request(
      'GET',
      '/users/me/weight-tracker',
      null,
      token,
    );
    handleResult(result);
    if (!result.ok) return;
    setWeightTrackerEntries(Array.isArray(result.data) ? result.data : []);
  }

  async function submitWeightTrackerEntry() {
    if (!accessToken) return;
    const value = Number(String(weightTrackerValue).trim().replace(',', '.'));
    if (!weightTrackerDate) return setErrorView('Укажи дату для веса.');
    if (!Number.isFinite(value) || value <= 0)
      return setErrorView('Укажи корректный вес в кг.');

    const result = await request(
      'POST',
      '/users/me/weight-tracker',
      { date: weightTrackerDate, weightKg: value },
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;
    if (Array.isArray(result.data)) {
      setWeightTrackerEntries(result.data);
    }
    await loadWeightTracker(accessToken);
    await loadMyProfile(accessToken);
    await syncLinkedAiExtrasFromProfileBody({
      birthDate: profileBirthDate || null,
      heightCm: profileToNumberOrNull(profileHeightCm),
      weightKg: value,
    });
    setWeightTrackerValue('');
  }

  async function confirmDeleteWeightTrackerEntry() {
    if (!accessToken || !pendingDeleteWeightEntry?.id) return;
    const entryId = pendingDeleteWeightEntry.id;
    const result = await request(
      'DELETE',
      `/users/me/weight-tracker/${entryId}`,
      null,
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;
    setPendingDeleteWeightEntry(null);
    if (Array.isArray(result.data)) {
      setWeightTrackerEntries(result.data);
    }
    await loadWeightTracker(accessToken);
    await loadMyProfile(accessToken);
    const pr = await request('GET', '/users/me', null, accessToken);
    if (pr.ok && pr.data) {
      await syncLinkedAiExtrasFromProfileBody({
        birthDate: pr.data.birthDate || null,
        heightCm: pr.data.heightCm,
        weightKg: pr.data.weightKg,
      });
    }
  }

  async function loadHabitsOverview(token = accessToken) {
    if (!token) return;
    const result = await request(
      'GET',
      '/users/me/habits/overview',
      null,
      token,
    );
    handleResult(result);
    if (!result.ok) return;

    setHabitsOverview(Array.isArray(result.data) ? result.data : []);
  }

  async function createHabit() {
    if (!accessToken) return;
    const name = String(habitsNewName ?? '').trim();
    if (!name) return setErrorView('Укажи название привычки.');
    const categoryId = habitsNewCategoryId || null;
    const daysRaw = String(habitsNewDaysToMaster ?? '').trim();
    const daysToMaster = daysRaw === '' ? 21 : Number(daysRaw);
    if (!Number.isFinite(daysToMaster) || daysToMaster < 0 || daysToMaster > 999) {
      return setErrorView('Дней до освоения: число от 0 до 999 (0 — без автоматической отметки).');
    }

    const result = await request(
      'POST',
      '/users/me/habits',
      { name, categoryId, daysToMaster },
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;

    setHabitsNewName('');
    setHabitsNewCategoryId('');
    setHabitsNewDaysToMaster('21');
    setIsCreateHabitModalOpen(false);
    await loadHabitsOverview(accessToken);
  }

  function openEditHabitModal(habit) {
    if (!habit?.id) return;
    setEditingHabit(habit);
    setHabitsEditName(String(habit.name ?? ''));
    setHabitsEditCategoryId(
      habit.categoryId ? String(habit.categoryId) : '',
    );
    setHabitsEditDaysToMaster(String(habit.daysToMaster ?? 21));
  }

  async function saveHabitEdit() {
    const habitId = editingHabit?.id;
    if (!accessToken || !habitId) return;
    const name = String(habitsEditName ?? '').trim();
    if (!name) return setErrorView('Укажи название привычки.');
    const categoryId = habitsEditCategoryId || null;
    const daysRaw = String(habitsEditDaysToMaster ?? '').trim();
    const daysToMaster = daysRaw === '' ? 21 : Number(daysRaw);
    if (!Number.isFinite(daysToMaster) || daysToMaster < 0 || daysToMaster > 999) {
      return setErrorView('Дней до освоения: число от 0 до 999.');
    }

    const result = await request(
      'PUT',
      `/users/me/habits/${habitId}`,
      { name, categoryId, daysToMaster },
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;

    setEditingHabit(null);
    await loadHabitsOverview(accessToken);
    const habitsToRefresh =
      activeHabitsCategoryGroup?.items?.length > 0
        ? activeHabitsCategoryGroup.items
        : habitsOverview;
    await loadHabitsCategoryCheckins(habitsToRefresh, accessToken);
  }

  async function confirmDeleteHabit() {
    const habitId = pendingDeleteHabit?.id;
    if (!accessToken || !habitId) return;

    const result = await request(
      'DELETE',
      `/users/me/habits/${habitId}`,
      null,
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;

    setPendingDeleteHabit(null);
    await loadHabitsOverview(accessToken);
  }

  async function loadHabitsCategoryCheckins(habits, token = accessToken) {
    if (!token) return;
    if (!habits?.length) {
      setHabitsCheckinsByHabitId({});
      return;
    }

    const query = new URLSearchParams();
    if (habitFilterDateFrom) query.set('from', habitFilterDateFrom);
    if (habitFilterDateTo) query.set('to', habitFilterDateTo);
    const suffix = query.toString() ? `?${query}` : '';

    const entries = await Promise.all(
      habits.map(async (h) => {
        const result = await request(
          'GET',
          `/users/me/habits/${h.id}/checkins${suffix}`,
          null,
          token,
        );
        if (!result.ok) return [String(h.id), {}];
        return [
          String(h.id),
          buildHabitCheckinsMap(
            Array.isArray(result.data) ? result.data : [],
          ),
        ];
      }),
    );
    setHabitsCheckinsByHabitId(Object.fromEntries(entries));
  }

  async function cycleHabitCheckinStatus(habit, date) {
    if (!accessToken || !habit?.id || !date) return;
    const habitKey = String(habit.id);
    const statusMap = habitsCheckinsByHabitId[habitKey] ?? {};
    const today = getTodayIsoDate();
    const current =
      date === today
        ? resolveHabitTodayStatus(statusMap, habit, today)
        : resolveHabitStatusForDate(statusMap, habit, date, null);
    const next = nextHabitCheckinStatus(current);

    const result = next
      ? await request(
          'POST',
          `/users/me/habits/${habit.id}/checkins`,
          { date, status: next },
          accessToken,
        )
      : await request(
          'DELETE',
          `/users/me/habits/${habit.id}/checkins?date=${date}`,
          null,
          accessToken,
        );
    handleResult(result);
    if (!result.ok) return;

    const habitsToRefresh =
      activeHabitsCategoryGroup?.items?.length > 0
        ? activeHabitsCategoryGroup.items
        : habitsOverview;
    await loadHabitsOverview(accessToken);
    await loadHabitsCategoryCheckins(habitsToRefresh, accessToken);
  }

  function applyHabitFilterPreset(days) {
    setHabitFilterDateFrom(getIsoDateDaysAgo(Math.max(0, days - 1)));
    setHabitFilterDateTo(getTodayIsoDate());
  }

  function applyTodoFilterPreset(preset) {
    if (preset === 'all') {
      setTodoFilterDateFrom('');
      setTodoFilterDateTo('');
      return;
    }
    if (preset === 'month') {
      const { from, to } = getCurrentCalendarMonthUtc();
      setTodoFilterDateFrom(from);
      setTodoFilterDateTo(to);
      return;
    }
    if (preset === 'week') {
      const { from, to } = getRollingWeekRange();
      setTodoFilterDateFrom(from);
      setTodoFilterDateTo(to);
      return;
    }
    if (preset === '3days') {
      setTodoFilterDateFrom(getIsoDateDaysAgo(2));
      setTodoFilterDateTo(getTodayIsoDate());
      return;
    }
    if (preset === 'weekend') {
      const { from, to } = getWeekendRangeUtc();
      setTodoFilterDateFrom(from);
      setTodoFilterDateTo(to);
    }
  }

  async function loadTodos(token = accessToken) {
    if (!token) return;
    const query = new URLSearchParams();
    if (todoFilterDateFrom) query.set('from', todoFilterDateFrom);
    if (todoFilterDateTo) query.set('to', todoFilterDateTo);
    const suffix = query.toString() ? `?${query}` : '';

    const result = await request('GET', `/users/me/todos${suffix}`, null, token);
    handleResult(result);
    if (!result.ok) return;
    setTodos(Array.isArray(result.data) ? result.data : []);
  }

  async function createTodo() {
    if (!accessToken) return;
    const title = String(todoTitleDraft ?? '').trim();
    if (!title) return setErrorView('Укажи название задачи.');

    const result = await request(
      'POST',
      '/users/me/todos',
      {
        title,
        dueDate: todoDueDateDraft ? todoDueDateDraft : null,
        categoryId: todoCategoryDraft || null,
      },
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;

    setTodoTitleDraft('');
    setTodoDueDateDraft('');
    setTodoCategoryDraft('');
    setIsCreateTodoModalOpen(false);
    await loadTodos(accessToken);
  }

  async function confirmDeleteTodo() {
    const todoId = pendingDeleteTodo?.id;
    if (!accessToken || !todoId) return;

    const result = await request(
      'DELETE',
      `/users/me/todos/${todoId}`,
      null,
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;

    setPendingDeleteTodo(null);
    await loadTodos(accessToken);
  }

  async function confirmCompleteTodo() {
    const todoId = pendingCompleteTodo?.id;
    if (!accessToken || !todoId) return;

    setPendingCompleteTodo(null);
    await upsertTodoDone(todoId, true);
  }

  async function upsertTodoDone(todoId, isDone) {
    if (!accessToken || !todoId) return;
    const body = isDone
      ? { isDone: true, date: getTodayIsoDate() }
      : { isDone: false };

    const result = await request(
      'PUT',
      `/users/me/todos/${todoId}/done`,
      body,
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;
    await loadTodos(accessToken);
  }

  function cycleTodoTableSort(key) {
    setTodoTableSort((prev) => {
      if (prev.key === key) {
        return { key, dir: prev.dir === 'asc' ? 'desc' : 'asc' };
      }
      return { key, dir: 'asc' };
    });
  }

  function cycleExerciseCatalogSort(key) {
    setExerciseCatalogSort((prev) => {
      if (prev.key === key) {
        return { key, dir: prev.dir === 'asc' ? 'desc' : 'asc' };
      }
      return { key, dir: 'asc' };
    });
  }

  function exerciseCatalogSortAria(key) {
    if (exerciseCatalogSort.key !== key) return 'none';
    return exerciseCatalogSort.dir === 'asc' ? 'ascending' : 'descending';
  }

  function compareTodoItems(a, b, key, dir) {
    const mult = dir === 'asc' ? 1 : -1;
    if (key === 'title') {
      return (
        mult * String(a.title ?? '').localeCompare(String(b.title ?? ''), 'ru')
      );
    }
    if (key === 'dueDate') {
      const aDate = a.dueDate ? String(a.dueDate) : '';
      const bDate = b.dueDate ? String(b.dueDate) : '';
      if (!aDate && !bDate) return 0;
      if (!aDate) return 1;
      if (!bDate) return -1;
      return mult * aDate.localeCompare(bDate);
    }
    if (key === 'status') {
      const aDone = a.doneDate ? 1 : 0;
      const bDone = b.doneDate ? 1 : 0;
      if (aDone !== bDone) return mult * (aDone - bDone);
      if (aDone) {
        return (
          mult *
          String(a.doneDate).localeCompare(String(b.doneDate))
        );
      }
      return (
        mult * String(a.title ?? '').localeCompare(String(b.title ?? ''), 'ru')
      );
    }
    if (key === 'category') {
      return (
        mult *
        String(a.categoryName ?? '').localeCompare(
          String(b.categoryName ?? ''),
          'ru',
        )
      );
    }
    return 0;
  }

  function todoTableSortAria(key) {
    if (todoTableSort.key !== key) return 'none';
    return todoTableSort.dir === 'asc' ? 'ascending' : 'descending';
  }

  async function loadWorkoutSessions(token = accessToken) {
    if (!token) return;
    const result = await request(
      'GET',
      '/users/me/workouts?includeHistory=false',
      null,
      token,
    );
    handleResult(result);
    if (!result.ok) return;
    setWorkoutSessions(Array.isArray(result.data) ? result.data : []);
  }

  async function loadWorkoutHistory(token = accessToken) {
    if (!token) return;
    const query = new URLSearchParams();
    if (historyDateFrom) query.set('from', historyDateFrom);
    if (historyDateTo) query.set('to', historyDateTo);
    const suffix = query.toString() ? `?${query}` : '';
    const result = await request(
      'GET',
      `/users/me/workouts/history${suffix}`,
      null,
      token,
    );
    handleResult(result);
    if (!result.ok) return;
    const logs = Array.isArray(result.data) ? result.data : [];
    const sortedLogs = [...logs].sort((a, b) => {
      const timeA = Date.parse(a?.date || a?._date || '') || 0;
      const timeB = Date.parse(b?.date || b?._date || '') || 0;
      return timeB - timeA;
    });
    setHistoryWorkoutLogs(sortedLogs);
  }

  async function loadWorkoutHistoryForStats(token = accessToken) {
    if (!token) return;
    const result = await request(
      'GET',
      '/users/me/workouts/history',
      null,
      token,
    );
    if (!result.ok) return;
    const logs = Array.isArray(result.data) ? result.data : [];
    const sortedLogs = [...logs].sort((a, b) => {
      const timeA = Date.parse(a?.date || a?._date || '') || 0;
      const timeB = Date.parse(b?.date || b?._date || '') || 0;
      return timeB - timeA;
    });
    setWorkoutHistoryForStats(sortedLogs);
  }

  function normalizeExerciseName(name) {
    return String(name || '')
      .trim()
      .toLowerCase();
  }

  function formatWorkoutSetPair(setItem) {
    const weight = String(setItem?.weight ?? '').trim();
    const reps = String(setItem?.reps ?? '').trim();
    if (weight && reps) return `${weight}×${reps}`;
    if (weight) return `${weight} кг`;
    if (reps) return `${reps} повт.`;
    return '';
  }

  function formatWorkoutSetsSummary(sets) {
    const parts = (sets || [])
      .map((setItem) => formatWorkoutSetPair(setItem))
      .filter(Boolean);
    return parts.length > 0 ? parts.join(', ') : '—';
  }

  function buildExerciseHistoryInsights(sessions) {
    const byName = new Map();
    const sorted = [...sessions].sort((a, b) => {
      const timeA = Date.parse(a?.date || a?._date || '') || 0;
      const timeB = Date.parse(b?.date || b?._date || '') || 0;
      return timeB - timeA;
    });

    for (const session of sorted) {
      const sessionDate = session.date || session._date;
      for (const exercise of session.exercises || []) {
        const key = normalizeExerciseName(exercise.name);
        if (!key) continue;

        let entry = byName.get(key);
        if (!entry) {
          entry = { lastSession: null };
          byName.set(key, entry);
        }

        const sets = exercise.sets || [];

        if (!entry.lastSession) {
          entry.lastSession = {
            date: sessionDate,
            sets: sets.map((setItem) => ({
              weight: setItem.weight || '',
              reps: setItem.reps || '',
              rpe: setItem.rpe || '',
            })),
          };
        }
      }
    }

    return byName;
  }

  async function loadWorkoutExerciseCatalog(token = accessToken) {
    if (!token) return;
    const result = await request(
      'GET',
      '/users/me/workouts/exercises',
      null,
      token,
    );
    handleResult(result);
    if (!result.ok) return;

    const incoming = Array.isArray(result.data) ? result.data : [];
    const unique = [];
    const seen = new Set();
    for (const item of incoming) {
      const key = (item.name || '').trim().toLowerCase();
      if (!key || seen.has(key)) continue;
      seen.add(key);
      unique.push(item);
    }
    setWorkoutExerciseCatalog(unique);
    if (!selectedCatalogExerciseId && unique[0]?.id) {
      setSelectedCatalogExerciseId(unique[0].id);
    }
  }

  function buildCatalogExerciseMeta(comment, muscleGroup, existingRawMeta = '') {
    const c = (comment || '').trim();
    const m = (muscleGroup || '').trim();
    const parsed = parseProgramExerciseMeta(existingRawMeta);
    if (parsed.sourceExerciseId) {
      return JSON.stringify({
        sourceExerciseId: parsed.sourceExerciseId,
        comment: c,
        muscleGroup: m,
      });
    }
    return c || m ? JSON.stringify({ comment: c, muscleGroup: m }) : '';
  }

  function openCreateCatalogExerciseModal() {
    setEditingCatalogExerciseId('');
    setNewCatalogExerciseName('');
    setNewCatalogExerciseMeta('');
    setNewCatalogExerciseMuscleGroup('');
    setIsCreateExerciseModalOpen(true);
  }

  function openEditCatalogExerciseModal(exercise) {
    if (!exercise) return;
    const parsed = parseProgramExerciseMeta(exercise.meta);
    setEditingCatalogExerciseId(exercise.id || '');
    setNewCatalogExerciseName(exercise.name || '');
    setNewCatalogExerciseMuscleGroup(parsed.muscleGroup || '');
    setNewCatalogExerciseMeta(
      parsed.isStructured ? parsed.comment : parsed.legacy,
    );
    setIsCreateExerciseModalOpen(true);
  }

  function closeCatalogExerciseModal() {
    setIsCreateExerciseModalOpen(false);
    setEditingCatalogExerciseId('');
    setNewCatalogExerciseName('');
    setNewCatalogExerciseMeta('');
    setNewCatalogExerciseMuscleGroup('');
  }

  async function saveCatalogExercise() {
    if (!accessToken) return;
    const name = (newCatalogExerciseName || '').trim();
    if (!name) return setErrorView('Укажи название упражнения.');

    const existingExercise = editingCatalogExerciseId
      ? workoutExerciseCatalog.find(
          (x) => String(x.id) === String(editingCatalogExerciseId),
        )
      : null;
    const meta = buildCatalogExerciseMeta(
      newCatalogExerciseMeta,
      newCatalogExerciseMuscleGroup,
      existingExercise?.meta || '',
    );

    if (editingCatalogExerciseId) {
      const result = await request(
        'PUT',
        `/users/me/workouts/exercises/${editingCatalogExerciseId}`,
        {
          name,
          meta,
          sets: [],
        },
        accessToken,
      );
      handleResult(result);
      if (!result.ok) return;
    } else {
      const seedCode = `catalog::${slugifyProgramCode(name)}-${Date.now()}`;
      const result = await request(
        'POST',
        '/users/me/workouts',
        {
          sessionCode: seedCode,
          date: new Date().toISOString().slice(0, 10),
          day: `Каталог: ${name}`,
          notes: 'Служебная запись для каталога упражнений',
          exercises: [
            {
              name,
              meta,
              sets: [],
            },
          ],
        },
        accessToken,
      );
      handleResult(result);
      if (!result.ok) return;
    }

    closeCatalogExerciseModal();
    await loadWorkoutExerciseCatalog(accessToken);
  }

  function openDeleteCatalogExerciseModal(exerciseId) {
    if (!exerciseId) return;
    setPendingDeleteCatalogExerciseId(exerciseId);
  }

  async function confirmDeleteCatalogExercise() {
    const exerciseId = pendingDeleteCatalogExerciseId;
    if (!accessToken || !exerciseId) return;

    const result = await request(
      'DELETE',
      `/users/me/workouts/exercises/${exerciseId}`,
      null,
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;

    if (String(selectedCatalogExerciseId) === String(exerciseId)) {
      setSelectedCatalogExerciseId('');
    }
    setPendingDeleteCatalogExerciseId(null);
    await loadWorkoutExerciseCatalog(accessToken);
  }

  function slugifyProgramCode(value) {
    const normalized = value
      .trim()
      .toLowerCase()
      .replace(/\s+/g, '-')
      .replace(/[^a-z0-9а-яё_-]/gi, '');
    return normalized || `program-${Date.now()}`;
  }

  function parseProgramExerciseMeta(rawMeta) {
    const raw = String(rawMeta || '').trim();
    if (!raw) {
      return {
        isStructured: false,
        sourceExerciseId: '',
        comment: '',
        muscleGroup: '',
        legacy: '',
      };
    }

    try {
      const parsed = JSON.parse(raw);
      if (
        parsed &&
        typeof parsed === 'object' &&
        ('sourceExerciseId' in parsed ||
          'comment' in parsed ||
          'muscleGroup' in parsed)
      ) {
        return {
          isStructured: true,
          sourceExerciseId: parsed?.sourceExerciseId
            ? String(parsed.sourceExerciseId)
            : '',
          comment: parsed?.comment ? String(parsed.comment) : '',
          muscleGroup: parsed?.muscleGroup ? String(parsed.muscleGroup) : '',
          legacy: '',
        };
      }
    } catch {
      // keep legacy plain text meta
    }

    return {
      isStructured: false,
      sourceExerciseId: '',
      comment: '',
      muscleGroup: '',
      legacy: raw,
    };
  }

  function addExerciseToProgram(exercise) {
    setProgramExercisesDraft((prev) => [
      ...prev,
      {
        id: `pex-${Date.now()}-${Math.random()}`,
        sourceExerciseId: exercise.id || '',
        name: exercise.name || '',
        comment: '',
      },
    ]);
    setProgramExercisePickerSearch('');
    setProgramExercisePickerOpen(false);
  }

  function selectProgramExerciseForPicker(exercise) {
    setProgramExercisePickerSearch(exercise.name || '');
    setProgramExercisePickerOpen(false);
  }

  function removeProgramExercise(exerciseId) {
    setProgramExercisesDraft((prev) => prev.filter((x) => x.id !== exerciseId));
  }

  function updateProgramExerciseComment(exerciseId, comment) {
    setProgramExercisesDraft((prev) =>
      prev.map((x) => (x.id === exerciseId ? { ...x, comment } : x)),
    );
  }

  async function saveProgramToDb() {
    if (!accessToken) return;
    const normalizedDay = programDay.trim();
    if (!normalizedDay) return setErrorView('Укажи название программы.');

    const code = `program::${slugifyProgramCode(programCode || normalizedDay)}`;
    const result = await request(
      'POST',
      '/users/me/workouts',
      {
        sessionCode: code,
        date: new Date().toISOString().slice(0, 10),
        day: normalizedDay,
        notes: programNotes.trim(),
        isActive: false,
        exercises: programExercisesDraft.map((x) => ({
          name: (x.name || '').trim(),
          meta: JSON.stringify({
            sourceExerciseId: x.sourceExerciseId || null,
            comment: (x.comment || '').trim(),
          }),
          sets: [],
        })),
      },
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;
    await loadWorkoutSessions(accessToken);
    setEditingProgramId('');
    setProgramCode('');
    setProgramDay('');
    setProgramNotes('');
    setProgramExercisesDraft([]);
    setProgramExercisePickerSearch('');
    setProgramExercisePickerOpen(false);
    setIsProgramModalOpen(false);
  }

  function openProgramCreateModal() {
    setEditingProgramId('');
    setProgramCode('');
    setProgramDay('');
    setProgramNotes('');
    setProgramExercisesDraft([]);
    setProgramExercisePickerSearch('');
    setProgramExercisePickerOpen(false);
    setIsProgramModalOpen(true);
  }

  function openProgramEditModal(program) {
    setEditingProgramId(program.id || '');
    setProgramCode((program.sessionCode || '').replace(/^program::/, ''));
    setProgramDay(program.day || '');
    setProgramNotes(program.notes || '');
    setProgramExercisesDraft(
      (program.exercises || []).map((x, idx) => ({
        ...parseProgramExerciseMeta(x.meta),
        id: `edit-${idx}-${Date.now()}`,
        name: x.name || '',
      })),
    );
    setProgramExercisePickerSearch('');
    setProgramExercisePickerOpen(false);
    setIsProgramModalOpen(true);
  }

  function closeProgramModal() {
    setIsProgramModalOpen(false);
    setProgramExercisePickerSearch('');
    setProgramExercisePickerOpen(false);
    setEditingProgramId('');
  }

  function openProgramDeleteModal(program) {
    setPendingDeleteProgram(program);
    setIsProgramDeleteModalOpen(true);
  }

  async function deleteProgramFromModal() {
    if (!pendingDeleteProgram?.id || !accessToken) return;
    const result = await request(
      'DELETE',
      `/users/me/workouts/${pendingDeleteProgram.id}`,
      null,
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;

    if (
      selectedProgramCode &&
      pendingDeleteProgram.sessionCode === selectedProgramCode
    ) {
      setSelectedProgramCode('');
    }
    setPendingDeleteProgram(null);
    setIsProgramDeleteModalOpen(false);
    await loadWorkoutSessions(accessToken);
  }

  function formatWorkoutDateLabel(iso) {
    if (!iso) return '';
    const raw = String(iso).slice(0, 10);
    try {
      const d = new Date(`${raw}T12:00:00`);
      if (Number.isNaN(d.getTime())) return raw;
      return d.toLocaleDateString('ru-RU', {
        day: 'numeric',
        month: 'long',
        year: 'numeric',
      });
    } catch {
      return raw;
    }
  }

  function formatBikeDurationSeconds(sec) {
    if (sec == null || !Number.isFinite(Number(sec))) return '—';
    const s = Math.round(Number(sec));
    const h = Math.floor(s / 3600);
    const m = Math.floor((s % 3600) / 60);
    const r = s % 60;
    if (h > 0)
      return `${h}:${String(m).padStart(2, '0')}:${String(r).padStart(2, '0')}`;
    return `${m}:${String(r).padStart(2, '0')}`;
  }

  function formatUtcDateTime(iso) {
    if (!iso) return '—';
    try {
      return new Date(iso).toLocaleString('ru-RU');
    } catch {
      return String(iso);
    }
  }

  function formatMoney(value, currency = 'RUB') {
    const amount = Number(value);
    if (!Number.isFinite(amount)) return '—';
    try {
      return new Intl.NumberFormat('ru-RU', {
        style: 'currency',
        currency: currency || 'RUB',
        maximumFractionDigits: 2,
      }).format(amount);
    } catch {
      return `${amount.toFixed(2)} ${currency || ''}`.trim();
    }
  }

  function formatInvestmentAssetType(value) {
    const map = {
      stock: 'Акции',
      bond: 'Облигации',
      etf: 'ETF',
      crypto: 'Крипто',
      other: 'Другое',
    };
    return map[String(value ?? '').toLowerCase()] || value || '—';
  }

  async function loadInvestments() {
    if (!accessToken) return;
    setInvestmentsMessage('');
    const [listResult, summaryResult] = await Promise.all([
      request('GET', '/users/me/investments', null, accessToken),
      request('GET', '/users/me/investments/summary', null, accessToken),
    ]);
    if (listResult.ok && Array.isArray(listResult.data)) {
      setInvestments(listResult.data);
    } else {
      handleResult(listResult);
    }
    if (summaryResult.ok) {
      setInvestmentSummary(summaryResult.data);
    } else {
      handleResult(summaryResult);
    }
  }

  function resetInvestmentDraft() {
    setInvestmentDraft({
      ticker: '',
      name: '',
      assetType: 'stock',
      quantity: '',
      averagePrice: '',
      currency: 'RUB',
      notes: '',
    });
  }

  async function submitCreateInvestment() {
    if (!accessToken) return;
    const ticker = investmentDraft.ticker.trim();
    const name = investmentDraft.name.trim();
    const quantity = Number(investmentDraft.quantity);
    const averagePrice = Number(investmentDraft.averagePrice);
    if (!ticker || !name) {
      setInvestmentsMessage('Укажите тикер и название.');
      return;
    }
    if (!Number.isFinite(quantity) || quantity <= 0) {
      setInvestmentsMessage('Количество должно быть больше нуля.');
      return;
    }
    if (!Number.isFinite(averagePrice) || averagePrice < 0) {
      setInvestmentsMessage('Укажите корректную среднюю цену.');
      return;
    }
    setInvestmentsMessage('');
    const result = await request(
      'POST',
      '/users/me/investments',
      {
        ticker,
        name,
        assetType: investmentDraft.assetType,
        quantity,
        averagePrice,
        currency: investmentDraft.currency || 'RUB',
        notes: investmentDraft.notes.trim() || null,
      },
      accessToken,
    );
    if (result.ok) {
      setIsCreateInvestmentModalOpen(false);
      resetInvestmentDraft();
      await loadInvestments();
      return;
    }
    setInvestmentsMessage(
      result.data?.message ||
        'Сохранение позиций пока недоступно — функция в разработке.',
    );
  }

  async function loadBikeActivities() {
    if (!accessToken) return;
    const qs = new URLSearchParams();
    qs.set('from', bikeDateFrom);
    qs.set('to', bikeDateTo);
    qs.set('sport', 'Biking');
    const result = await request(
      'GET',
      `/users/me/bike-activities?${qs}`,
      null,
      accessToken,
    );
    handleResult(result);
    if (result.ok && Array.isArray(result.data)) {
      setBikeActivities(result.data);
    }
  }

  async function importBikeTcxFile(file) {
    if (!accessToken || !file) return;
    setActiveApiRequests((p) => p + 1);
    setBikeImportMessage('');
    try {
      const fd = new FormData();
      fd.append('file', file);
      const url = `${baseUrl.trim().replace(/\/+$/, '')}/users/me/bike-activities/import`;
      const response = await fetch(url, {
        method: 'POST',
        headers: { Authorization: `Bearer ${accessToken}` },
        body: fd,
      });
      const data = await response.json().catch(() => ({}));
      const result = { ok: response.ok, status: response.status, data };
      handleResult(result);
      if (result.ok) {
        setBikeImportMessage('Файл успешно импортирован.');
        await loadBikeActivities();
      } else {
        setBikeImportMessage(data.message || JSON.stringify(data, null, 2));
      }
    } catch (error) {
      setBikeImportMessage(String(error));
    } finally {
      setActiveApiRequests((p) => Math.max(0, p - 1));
      if (bikeTcxImportRef.current) bikeTcxImportRef.current.value = '';
    }
  }

  async function openBikeActivityDetail(id) {
    if (!accessToken) return;
    setBikeDetailOpen(true);
    setBikeDetailLoading(true);
    setBikeDetail(null);
    const result = await request(
      'GET',
      `/users/me/bike-activities/${id}?trackpointLimit=5000`,
      null,
      accessToken,
    );
    setBikeDetailLoading(false);
    if (result.ok) {
      setBikeDetail(result.data);
    } else {
      handleResult(result);
      setBikeDetailOpen(false);
    }
  }

  async function deleteBikeActivityConfirmed() {
    if (!pendingDeleteBikeActivityId || !accessToken) return;
    const deletedId = pendingDeleteBikeActivityId;
    const result = await request(
      'DELETE',
      `/users/me/bike-activities/${deletedId}`,
      null,
      accessToken,
    );
    handleResult(result);
    if (result.ok || result.status === 204) {
      setPendingDeleteBikeActivityId(null);
      if (bikeDetail?.id === deletedId) {
        setBikeDetail(null);
        setBikeDetailOpen(false);
      }
      await loadBikeActivities();
    }
  }

  function downloadJsonFile(fileName, payload) {
    const content = JSON.stringify(payload, null, 2);
    const blob = new Blob([content], {
      type: 'application/json;charset=utf-8',
    });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  function mapServerSessionToCurrentWorkout(session) {
    const dateStr = session.date || session._date;
    return {
      sessionId: session.id,
      sessionCode: session.sessionCode,
      day: session.day || '',
      date: dateStr
        ? String(dateStr).slice(0, 10)
        : new Date().toISOString().slice(0, 10),
      notes: session.notes || '',
      isActive: session.isActive !== false,
      exercises: (session.exercises || []).map((x) => {
        const setsSource =
          x.sets && x.sets.length > 0
            ? x.sets
            : [{ weight: '', reps: '', rpe: '8' }];
        return {
          id: String(x.id),
          name: x.name || '',
          meta: x.meta || '',
          sets: setsSource.map((s) => ({
            weight: s.weight ?? '',
            reps: s.reps ?? '',
            rpe: s.rpe || '8',
          })),
        };
      }),
    };
  }

  function startWorkoutFromProgram(program) {
    loadWorkoutHistoryForStats();
    setCurrentWorkout({
      sessionCode: `workout::${Date.now()}`,
      day: program.day || 'Тренировка',
      date: new Date().toISOString().slice(0, 10),
      notes: '',
      isActive: true,
      exercises: (program.exercises || []).map((x, idx) => {
        const parsedMeta = parseProgramExerciseMeta(x.meta);
        return {
          id: `cw-${idx}-${Date.now()}`,
          name: x.name || '',
          meta: parsedMeta.comment || parsedMeta.legacy,
          sets: [{ weight: '', reps: '', rpe: '' }],
        };
      }),
    });
    setIsActiveWorkoutModalOpen(true);
  }

  function createWorkoutFromScratch() {
    loadWorkoutHistoryForStats();
    setCurrentWorkout({
      sessionCode: `workout::${Date.now()}`,
      day: 'Новая тренировка',
      date: new Date().toISOString().slice(0, 10),
      notes: '',
      isActive: true,
      exercises: [],
    });
    setIsActiveWorkoutModalOpen(true);
  }

  async function downloadPlanningJsonTemplate() {
    if (!accessToken) return;
    const result = await request(
      'GET',
      '/users/me/workouts/planning/import-template',
      null,
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;

    const templateJson = JSON.stringify(result.data || {}, null, 2);
    const blob = new Blob([templateJson], {
      type: 'application/json;charset=utf-8',
    });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'workout-planning-template.json';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  async function downloadExercisesJsonTemplate() {
    if (!accessToken) return;
    const result = await request(
      'GET',
      '/users/me/workouts/exercises/import-template',
      null,
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;

    const templateJson = JSON.stringify(result.data || {}, null, 2);
    const blob = new Blob([templateJson], {
      type: 'application/json;charset=utf-8',
    });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'workout-exercises-template.json';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  async function exportExercisesIdNameJson() {
    if (!accessToken) return;
    const result = await request(
      'GET',
      '/users/me/workouts/exercises/export',
      null,
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;

    const payload = {
      exercises: Array.isArray(result.data?.exercises)
        ? result.data.exercises
        : [],
    };

    const content = JSON.stringify(payload, null, 2);
    const blob = new Blob([content], {
      type: 'application/json;charset=utf-8',
    });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'workout-exercises-id-name.json';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  function openPlanningImportDialog() {
    planningImportInputRef.current?.click();
  }

  function openExercisesImportDialog() {
    exercisesImportInputRef.current?.click();
  }

  async function importWorkoutPlanningFromJson(rawText) {
    let parsed;
    try {
      parsed = JSON.parse(rawText);
    } catch {
      setErrorView('Некорректный JSON. Проверь файл и попробуй снова.');
      return;
    }

    const payload = Array.isArray(parsed)
      ? { programs: parsed }
      : Array.isArray(parsed?.programs)
        ? parsed
        : null;
    if (!payload) {
      setErrorView(
        'Ожидается JSON с массивом программ: { "programs": [...] }.',
      );
      return;
    }

    const result = await request(
      'POST',
      '/users/me/workouts/planning/import',
      payload,
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;

    await loadWorkoutSessions(accessToken);
    openStrengthSubTab('manage');
    setWorkoutsManageSubTab('add');
  }

  async function importWorkoutExercisesFromJson(rawText) {
    let parsed;
    try {
      parsed = JSON.parse(rawText);
    } catch {
      setErrorView('Некорректный JSON. Проверь файл и попробуй снова.');
      return;
    }

    const payload = Array.isArray(parsed)
      ? { exercises: parsed }
      : Array.isArray(parsed?.exercises)
        ? parsed
        : null;
    if (!payload) {
      setErrorView(
        'Ожидается JSON с массивом упражнений: { "exercises": [...] }.',
      );
      return;
    }

    const result = await request(
      'POST',
      '/users/me/workouts/exercises/import',
      payload,
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;

    await loadWorkoutExerciseCatalog(accessToken);
    openStrengthSubTab('manage');
    setWorkoutsManageSubTab('exercises');
  }

  async function handlePlanningJsonImportChange(event) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;

    const text = await file.text();
    await importWorkoutPlanningFromJson(text);
  }

  async function handleExercisesJsonImportChange(event) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;

    const text = await file.text();
    await importWorkoutExercisesFromJson(text);
  }

  function hideActiveWorkoutModal() {
    setIsActiveWorkoutModalOpen(false);
    setActiveWorkoutCollapsedExerciseIds({});
  }

  function toggleActiveWorkoutExerciseCollapsed(exerciseId) {
    setActiveWorkoutCollapsedExerciseIds((prev) => {
      if (prev[exerciseId]) {
        const next = { ...prev };
        delete next[exerciseId];
        return next;
      }
      return { ...prev, [exerciseId]: true };
    });
  }

  function openOrResumeWorkoutModal() {
    if (!currentWorkout) {
      const fromList = workoutSessions.find(
        (s) =>
          String(s.sessionCode || '').startsWith('workout::') &&
          s.isActive === true,
      );
      if (fromList) {
        setCurrentWorkout(mapServerSessionToCurrentWorkout(fromList));
      }
    }
    loadWorkoutHistoryForStats();
    setIsActiveWorkoutModalOpen(true);
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
            name: '',
            meta: '',
            sets: [{ weight: '', reps: '', rpe: '' }],
          },
        ],
      };
    });
  }

  function updateCurrentWorkoutExercise(exerciseId, field, value) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.map((x) =>
          x.id === exerciseId ? { ...x, [field]: value } : x,
        ),
      };
    });
  }

  function removeCurrentWorkoutExercise(exerciseId) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.filter((x) => x.id !== exerciseId),
      };
    });
  }

  function moveCurrentWorkoutExercise(exerciseId, direction) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      const idx = prev.exercises.findIndex((x) => x.id === exerciseId);
      if (idx < 0) return prev;
      const newIdx = idx + direction;
      if (newIdx < 0 || newIdx >= prev.exercises.length) return prev;
      const next = [...prev.exercises];
      [next[idx], next[newIdx]] = [next[newIdx], next[idx]];
      return { ...prev, exercises: next };
    });
  }

  function openDeleteCurrentWorkoutExerciseModal(exerciseId) {
    if (!exerciseId) return;
    setPendingDeleteCurrentWorkoutExerciseId(exerciseId);
  }

  function confirmDeleteCurrentWorkoutExercise() {
    if (!pendingDeleteCurrentWorkoutExerciseId) return;
    removeCurrentWorkoutExercise(pendingDeleteCurrentWorkoutExerciseId);
    setPendingDeleteCurrentWorkoutExerciseId(null);
  }

  function updateCurrentWorkoutSet(exerciseId, setIndex, field, value) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.map((x) =>
          x.id === exerciseId
            ? {
                ...x,
                sets: x.sets.map((s, idx) =>
                  idx === setIndex ? { ...s, [field]: value } : s,
                ),
              }
            : x,
        ),
      };
    });
  }

  function adjustCurrentWorkoutSetWeight(exerciseId, setIndex, delta) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.map((x) => {
          if (x.id !== exerciseId) return x;
          return {
            ...x,
            sets: x.sets.map((s, idx) => {
              if (idx !== setIndex) return s;
              const currentWeightRaw = String(s.weight ?? '')
                .replace(',', '.')
                .trim();
              const currentWeight = Number(currentWeightRaw);
              const base = Number.isNaN(currentWeight) ? 0 : currentWeight;
              const nextValue = Math.max(
                0,
                Math.round((base + delta) * 100) / 100,
              );
              return { ...s, weight: String(nextValue) };
            }),
          };
        }),
      };
    });
  }

  function adjustCurrentWorkoutSetReps(exerciseId, setIndex, delta) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.map((x) => {
          if (x.id !== exerciseId) return x;
          return {
            ...x,
            sets: x.sets.map((s, idx) => {
              if (idx !== setIndex) return s;
              const currentRepsRaw = String(s.reps ?? '').trim();
              const currentReps = Number(currentRepsRaw);
              const base = Number.isNaN(currentReps) ? 0 : currentReps;
              const nextValue = Math.max(0, Math.round(base + delta));
              return { ...s, reps: String(nextValue) };
            }),
          };
        }),
      };
    });
  }

  function adjustCurrentWorkoutSetRpe(exerciseId, setIndex, delta) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.map((x) => {
          if (x.id !== exerciseId) return x;
          return {
            ...x,
            sets: x.sets.map((s, idx) => {
              if (idx !== setIndex) return s;
              const currentRpe = Number(String(s.rpe ?? '8').trim());
              const base = Number.isNaN(currentRpe) ? 8 : currentRpe;
              const nextValue = Math.min(10, Math.max(6, base + delta));
              return { ...s, rpe: String(nextValue) };
            }),
          };
        }),
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
            ? {
                ...x,
                sets: [
                  ...x.sets,
                  {
                    weight: x.sets[x.sets.length - 1]?.weight ?? '',
                    reps: x.sets[x.sets.length - 1]?.reps ?? '',
                    rpe: '',
                  },
                ],
              }
            : x,
        ),
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
            : x,
        ),
      };
    });
  }

  async function persistCurrentWorkout(finish) {
    if (!accessToken || !currentWorkout) return;
    if (!(currentWorkout.day || '').trim())
      return setErrorView('Укажи название тренировки.');
    const result = await request(
      'POST',
      '/users/me/workouts',
      {
        sessionCode: currentWorkout.sessionCode,
        date: (
          currentWorkout.date || new Date().toISOString().slice(0, 10)
        ).slice(0, 10),
        day: currentWorkout.day.trim(),
        notes: currentWorkout.notes || '',
        isActive: !finish,
        exercises: currentWorkout.exercises
          .map((x) => ({
            name: (x.name || '').trim(),
            meta: x.meta,
            sets: x.sets.map((s) => ({
              weight: s.weight,
              reps: s.reps,
              rpe: s.rpe,
            })),
          }))
          .filter((x) => x.name),
      },
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;
    if (result.data) {
      setCurrentWorkout(mapServerSessionToCurrentWorkout(result.data));
    }
    if (finish) {
      setCurrentWorkout(null);
    }
    setIsActiveWorkoutModalOpen(false);
    await loadWorkoutSessions(accessToken);
  }

  function openDeleteWorkoutLogModal(sessionId) {
    if (!sessionId) return;
    setPendingDeleteWorkoutSessionId(sessionId);
  }

  function openWorkoutHistoryModal(session) {
    if (!session) return;
    setSelectedWorkoutHistorySession(session);
  }

  function closeWorkoutHistoryModal() {
    setSelectedWorkoutHistorySession(null);
  }

  function applyDateRangePreset(days, setFrom, setTo) {
    if (!days || days < 1) return;
    if (days === 7) {
      const { from, to } = getRollingWeekRange();
      setFrom(from);
      setTo(to);
      return;
    }
    const end = new Date();
    end.setHours(0, 0, 0, 0);
    const start = new Date(end);
    start.setDate(start.getDate() - (days - 1));
    setFrom(start.toISOString().slice(0, 10));
    setTo(end.toISOString().slice(0, 10));
  }

  function applyHistoryDatePreset(days) {
    applyDateRangePreset(days, setHistoryDateFrom, setHistoryDateTo);
  }

  function applyWeightDatePreset(days) {
    applyDateRangePreset(days, setWeightFilterDateFrom, setWeightFilterDateTo);
  }

  function applyBikeDatePreset(days) {
    applyDateRangePreset(days, setBikeDateFrom, setBikeDateTo);
  }

  function exportHistorySessionJson(session) {
    if (!session) return;
    const datePart =
      String(session.date || session._date || '').slice(0, 10) ||
      'unknown-date';
    const idPart = String(session.id || 'workout');
    downloadJsonFile(`workout-history-${datePart}-${idPart}.json`, session);
  }

  function exportHistorySelectionJson() {
    const fromPart = historyDateFrom || 'all';
    const toPart = historyDateTo || 'all';
    downloadJsonFile(`workout-history-${fromPart}-to-${toPart}.json`, {
      exportedAt: new Date().toISOString(),
      filters: {
        from: historyDateFrom || null,
        to: historyDateTo || null,
      },
      total: historyWorkoutLogs.length,
      workouts: historyWorkoutLogs,
    });
  }

  function exportWeightSelectionJson() {
    const entries = weightTrackerEntries.filter((row) => {
      const d = String(row.date || '').slice(0, 10);
      if (!d) return false;
      if (weightFilterDateFrom && d < weightFilterDateFrom) return false;
      if (weightFilterDateTo && d > weightFilterDateTo) return false;
      return true;
    });
    const fromPart = weightFilterDateFrom || 'all';
    const toPart = weightFilterDateTo || 'all';
    downloadJsonFile(`weight-tracker-${fromPart}-to-${toPart}.json`, {
      exportedAt: new Date().toISOString(),
      filters: {
        from: weightFilterDateFrom || null,
        to: weightFilterDateTo || null,
      },
      total: entries.length,
      entries,
    });
  }

  async function confirmDeleteWorkoutSession() {
    const sessionId = pendingDeleteWorkoutSessionId;
    if (!accessToken) return;
    if (!sessionId) return;

    const result = await request(
      'DELETE',
      `/users/me/workouts/${sessionId}`,
      null,
      accessToken,
    );
    handleResult(result);
    if (!result.ok) return;

    setPendingDeleteWorkoutSessionId(null);
    if (selectedWorkoutHistorySession?.id === sessionId) {
      setSelectedWorkoutHistorySession(null);
    }
    if (
      selectedProgramCode &&
      workoutPrograms.some(
        (x) => x.id === sessionId && x.sessionCode === selectedProgramCode,
      )
    ) {
      setSelectedProgramCode('');
    }
    await loadWorkoutSessions(accessToken);
    await loadWorkoutHistory(accessToken);
  }

  async function saveMyProfile() {
    if (!accessToken) return;

    const body = {
      birthDate: profileBirthDate || null,
      heightCm: profileToNumberOrNull(profileHeightCm),
      weightKg: profileToNumberOrNull(profileWeightKg),
      phone: profilePhone.trim() || null,
      city: profileCity.trim() || null,
      about: profileAbout.trim() || null,
      firstName: profileFirstName.trim() || null,
      lastName: profileLastName.trim() || null,
    };

    const result = await request('PUT', '/users/me', body, accessToken);
    handleResult(result);
    if (result.ok) {
      setIsProfileEditModalOpen(false);
      await loadMyProfile(accessToken);
      await syncLinkedAiExtrasFromProfileBody(body);
    }
  }

  async function loadAdminDialogs(
    forceUserId = adminDialogUserId,
    forceDialogId = '',
  ) {
    if (!adminToken) return;
    const query = forceUserId
      ? `?userId=${encodeURIComponent(forceUserId)}`
      : '';
    const result = await request(
      'GET',
      `/admin/dialogs${query}`,
      null,
      adminToken,
    );
    handleResult(result);
    if (!result.ok) return;
    const incoming = Array.isArray(result.data) ? result.data : [];
    setAdminDialogs(incoming);
    const nextDialogId =
      forceDialogId ||
      (incoming.some((d) => d.id === adminCurrentDialogId)
        ? adminCurrentDialogId
        : incoming[0]?.id || '');
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
    const result = await request(
      'GET',
      `/admin/dialogs/${dialogId}/messages`,
      null,
      adminToken,
    );
    handleResult(result);
    if (!result.ok) return;
    setAdminDialogMessages(Array.isArray(result.data) ? result.data : []);
  }

  async function submitCreateAdminDialog() {
    if (!adminDialogUserId)
      return setErrorView('Выбери пользователя для нового диалога.');
    const title = (adminDialogTitleDraft || '').trim() || 'Новый диалог';
    const result = await request(
      'POST',
      '/admin/dialogs',
      { userId: adminDialogUserId, title },
      adminToken,
    );
    handleResult(result);
    if (result.ok) {
      setAdminDialogModalKind(null);
      setAdminDialogTitleDraft('');
      await loadAdminDialogs(adminDialogUserId, result.data?.id);
    }
  }

  async function submitRenameAdminDialog() {
    if (!adminCurrentDialogId) return setErrorView('Сначала выбери диалог.');
    const title = (adminDialogTitleDraft || '').trim();
    if (!title) return setErrorView('Введи название.');
    const result = await request(
      'PUT',
      `/admin/dialogs/${adminCurrentDialogId}`,
      { title },
      adminToken,
    );
    handleResult(result);
    if (result.ok) {
      setAdminDialogModalKind(null);
      setAdminDialogTitleDraft('');
      await loadAdminDialogs(adminDialogUserId, adminCurrentDialogId);
    }
  }

  async function submitDeleteAdminDialog() {
    if (!adminCurrentDialogId) return setErrorView('Сначала выбери диалог.');
    const deletingId = adminCurrentDialogId;
    const result = await request(
      'DELETE',
      `/admin/dialogs/${deletingId}`,
      null,
      adminToken,
    );
    handleResult(result);
    if (result.ok) {
      setAdminDialogModalKind(null);
      await loadAdminDialogs(adminDialogUserId);
    }
  }

  async function createAdminUser() {
    const result = await request(
      'POST',
      '/admin/users',
      {
        username: adminCreateUsername.trim(),
        password: adminCreatePassword,
        roles: adminCreateRoles,
      },
      adminToken,
    );
    handleResult(result);
    if (result.ok) {
      setAdminCreateUsername('');
      setAdminCreatePassword('');
      setAdminCreateRoles(['AiUser']);
      setIsCreateUserModalOpen(false);
      await loadUsers();
    }
  }

  async function saveAdminUserFromModal() {
    if (!selectedAdminUser) return;
    const result = await request(
      'PUT',
      `/admin/users/${selectedAdminUser.id}`,
      { username: editUserName.trim(), roles: editUserRoles },
      adminToken,
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
    if (!newPasswordValue.trim()) return setErrorView('Введи новый пароль.');
    const result = await request(
      'PUT',
      `/admin/users/${selectedAdminUser.id}/password`,
      { password: newPasswordValue },
      adminToken,
    );
    handleResult(result);
    if (result.ok) {
      setNewPasswordValue('');
      setIsPasswordModalOpen(false);
      setSelectedAdminUser(null);
    }
  }

  async function deleteAdminUserFromModal() {
    if (!selectedAdminUser) return;
    const result = await request(
      'DELETE',
      `/admin/users/${selectedAdminUser.id}`,
      null,
      adminToken,
    );
    handleResult(result);
    if (result.ok) {
      setIsDeleteModalOpen(false);
      setSelectedAdminUser(null);
      await loadUsers();
    }
  }

  function openEditModal(user) {
    setSelectedAdminUser(user);
    setEditUserName(user.username || '');
    setEditUserRoles(normalizeUserRoles(user));
    setIsEditUserModalOpen(true);
  }

  function openPasswordModal(user) {
    setSelectedAdminUser(user);
    setNewPasswordValue('');
    setIsPasswordModalOpen(true);
  }

  function openDeleteModal(user) {
    setSelectedAdminUser(user);
    setIsDeleteModalOpen(true);
  }

  const roleLabelsMap = useMemo(() => {
    const map = {
      User: 'Пользователь',
      AiUser: 'AI',
      Admin: 'Админ',
    };
    for (const row of adminRoles) {
      map[row.name] = row.label || row.name;
    }
    return map;
  }, [adminRoles]);

  const userRoleOptions = useMemo(() => {
    if (!adminRoles.length) {
      return [
        { value: 'User', label: 'Пользователь' },
        { value: 'AiUser', label: 'AI-пользователь' },
        { value: 'Admin', label: 'Администратор' },
      ];
    }
    return [...adminRoles]
      .filter((row) => row.isActive)
      .sort((a, b) => (a.sortOrder || 0) - (b.sortOrder || 0))
      .map((row) => ({ value: row.name, label: row.label || row.name }));
  }, [adminRoles]);

  const defaultUserRoleName = useMemo(() => {
    if (adminRoles.some((row) => row.name === 'User' && row.isActive)) {
      return 'User';
    }
    const first = adminRoles.find((row) => row.isActive);
    return first?.name || 'User';
  }, [adminRoles]);

  const formatUserRolesLabel = (roles) =>
    roles.map((role) => roleLabelsMap[role] || role).join(', ');

  const toggleUserRoleSelection = (current, role, checked) => {
    if (checked) {
      return current.includes(role) ? current : [...current, role];
    }
    const next = current.filter((item) => item !== role);
    return next.length > 0 ? next : [defaultUserRoleName];
  };

  const dialogOptions = useMemo(
    () =>
      dialogs.map((d) => (
        <option key={d.id} value={d.id}>
          {d.title || 'Диалог'}
        </option>
      )),
    [dialogs],
  );
  const adminUserOptions = useMemo(
    () =>
      users.map((u) => (
        <option key={u.id} value={u.id}>
          {u.username} ({formatUserRolesLabel(normalizeUserRoles(u))})
        </option>
      )),
    [users, roleLabelsMap],
  );
  const adminDialogOptions = useMemo(
    () =>
      adminDialogs.map((d) => {
        const assistantLabel = (d.aiAssistantName || '').trim()
          ? d.aiAssistantName
          : 'без помощника';
        return (
          <option key={d.id} value={d.id}>
            {d.title || 'Диалог'} ({d.username || 'неизвестно'}) —{' '}
            {assistantLabel}
          </option>
        );
      }),
    [adminDialogs],
  );
  const adminSelectedDialogAssistantLabel = useMemo(() => {
    const d = adminDialogs.find((x) => x.id === adminCurrentDialogId);
    if (!d) return '';
    if ((d.aiAssistantName || '').trim()) return d.aiAssistantName;
    return 'в диалоге не было помощника';
  }, [adminDialogs, adminCurrentDialogId]);
  const adminManageSelectedUser = useMemo(
    () => users.find((u) => u.id === adminManageUserId) || null,
    [users, adminManageUserId],
  );
  const workoutPrograms = useMemo(
    () =>
      workoutSessions.filter((x) =>
        String(x.sessionCode || '').startsWith('program::'),
      ),
    [workoutSessions],
  );
  const activeWorkoutSession = useMemo(
    () =>
      workoutSessions.find(
        (x) =>
          String(x.sessionCode || '').startsWith('workout::') &&
          x.isActive === true,
      ),
    [workoutSessions],
  );

  const isLoggedIn = Boolean(accessToken);
  const hasPermission = (code) =>
    currentUserPermissions.some(
      (item) => String(item).toLowerCase() === String(code).toLowerCase(),
    );
  const hasAdminAccess = currentUserPermissions.some((item) =>
    String(item).startsWith('admin.'),
  );
  const hasAiAccess = hasPermission(APP_PERMISSION.AiAssistant);
  const isAdmin = hasAdminAccess;

  const sidebarNavTabs = useMemo(
    () =>
      [
        { id: 'workouts', label: 'Силовые', permission: APP_PERMISSION.Workouts },
        { id: 'bike', label: 'Вело', permission: APP_PERMISSION.Bike },
        { id: 'progress', label: 'Мой прогресс', permission: APP_PERMISSION.Progress },
        { id: 'habits', label: 'Привычки', permission: APP_PERMISSION.Habits },
        { id: 'todos', label: 'Задачи', permission: APP_PERMISSION.Todos },
        {
          id: 'investments',
          label: 'Инвестиции',
          permission: APP_PERMISSION.Investments,
        },
      ].filter((item) => hasPermission(item.permission)),
    [currentUserPermissions],
  );

  const accountNavItems = useMemo(
    () =>
      [
        { id: 'profile', label: 'Профиль', permission: APP_PERMISSION.Profile },
        { id: 'ai', label: 'AI помощник', permission: APP_PERMISSION.AiAssistant },
        { id: 'admin', label: 'Админ', permission: null },
      ].filter((item) => {
        if (item.id === 'admin') return hasAdminAccess;
        return hasPermission(item.permission);
      }),
    [currentUserPermissions, hasAdminAccess],
  );

  const visibleAdminSubTabs = useMemo(
    () =>
      Object.entries(ADMIN_SUBTAB_PERMISSION)
        .filter(([, permission]) => hasPermission(permission))
        .map(([id]) => id),
    [currentUserPermissions],
  );

  useEffect(() => {
    if (tab !== 'admin' || !hasAdminAccess) return;
    if (
      visibleAdminSubTabs.length > 0 &&
      !visibleAdminSubTabs.includes(adminSubTab)
    ) {
      setAdminSubTab(visibleAdminSubTabs[0]);
    }
  }, [tab, hasAdminAccess, adminSubTab, visibleAdminSubTabs]);

  const adminSubTabLabels = {
    'users-manage': 'Учётные записи',
    roles: 'Роли',
    categories: 'Категории',
    'users-profiles': 'Профили',
    'ai-assistants': 'ИИ помощники',
    'ai-test-chat': 'Тест чата',
    dialogs: 'Диалоги',
  };

  const permissionsByCategory = useMemo(() => {
    const grouped = {};
    for (const row of adminPermissionsCatalog) {
      const category = row.category || 'other';
      if (!grouped[category]) grouped[category] = [];
      grouped[category].push(row);
    }
    for (const category of Object.keys(grouped)) {
      grouped[category].sort(
        (a, b) => (a.sortOrder || 0) - (b.sortOrder || 0),
      );
    }
    return grouped;
  }, [adminPermissionsCatalog]);

  const topbarDisplayName = useMemo(
    () =>
      formatUserDisplayName(
        profileFirstName,
        profileLastName,
        currentUserName || username,
      ),
    [profileFirstName, profileLastName, currentUserName, username],
  );

  const workoutTrainerAssistant = useMemo(
    () => aiAssistants.find((x) => x?.assistantCode === 'trainer') ?? null,
    [aiAssistants],
  );
  const showWorkoutAiTrainerTab = Boolean(workoutTrainerAssistant?.selected);

  /** Пока открыта модалка доп. полей ИИ — подтягиваем вес / рост / возраст из профиля, не затирая сохранённые значения, если в профиле пусто. */
  useEffect(() => {
    if (!isAiExtraInfoModalOpen) return;
    const keys = new Set(aiExtraInfoDefinitions.map((d) => d.fieldKey));
    if (!keys.has('weight') && !keys.has('height') && !keys.has('age')) return;
    setAiExtraInfoValues((prev) => mergeAiExtrasWithProfile(prev, keys));
  }, [
    profileBirthDate,
    profileHeightCm,
    profileWeightKg,
    isAiExtraInfoModalOpen,
    aiExtraInfoDefinitions,
  ]);

  useEffect(() => {
    if (
      tab !== 'workouts' ||
      workoutsSubTab !== 'strength' ||
      strengthSubTab !== 'my-workout'
    )
      return;
    const active = workoutSessions.find(
      (s) =>
        String(s.sessionCode || '').startsWith('workout::') &&
        s.isActive === true,
    );
    if (active && !currentWorkout) {
      setCurrentWorkout(mapServerSessionToCurrentWorkout(active));
    }
  }, [tab, workoutsSubTab, strengthSubTab, workoutSessions, currentWorkout]);
  useEffect(() => {
    if (tab !== 'workouts' || !accessToken) return;
    loadWorkoutSessions();
    loadWorkoutExerciseCatalog();
  }, [tab, accessToken]);
  useEffect(() => {
    if (tab !== 'workouts' || workoutsSubTab !== 'strength' || !accessToken) {
      return;
    }
    if (strengthSubTab !== 'my-workout' && !isActiveWorkoutModalOpen) return;
    loadWorkoutHistoryForStats();
  }, [
    tab,
    workoutsSubTab,
    strengthSubTab,
    isActiveWorkoutModalOpen,
    accessToken,
  ]);
  useEffect(() => {
    if (
      tab !== 'workouts' ||
      workoutsSubTab !== 'strength' ||
      strengthSubTab !== 'history'
    )
      return;
    loadWorkoutHistory();
  }, [tab, workoutsSubTab, strengthSubTab]);
  useEffect(() => {
    if (
      tab !== 'workouts' ||
      workoutsSubTab !== 'strength' ||
      strengthSubTab !== 'history'
    )
      return;
    loadWorkoutHistory();
  }, [tab, workoutsSubTab, strengthSubTab, historyDateFrom, historyDateTo]);
  useEffect(() => {
    if (tab !== 'bike' || !accessToken) return;
    loadBikeActivities();
  }, [tab, accessToken, bikeDateFrom, bikeDateTo]);
  useEffect(() => {
    if (tab !== 'investments' || !accessToken) return;
    if (!hasPermission(APP_PERMISSION.Investments)) return;
    loadInvestments();
  }, [tab, accessToken, currentUserPermissions]);
  useEffect(() => {
    if (tab !== 'workouts' || !hasAiAccess || !aiToken) return;
    loadAiAssistants(aiToken);
  }, [tab, hasAiAccess, aiToken]);
  useEffect(() => {
    if (tab !== 'workouts' || workoutsSubTab !== 'ai-trainer') return;
    if (!aiToken) return;
    loadDialogs(aiToken);
  }, [tab, workoutsSubTab, aiToken]);
  useEffect(() => {
    if (tab !== 'workouts' || workoutsSubTab !== 'weekly-reviews') return;
    if (!aiToken) return;
    loadWeeklyTrainingReviews(aiToken);
  }, [tab, workoutsSubTab, aiToken]);
  useEffect(() => {
    if (tab !== 'habits' || !accessToken) return;
    void loadHabitsOverview(accessToken);
  }, [tab, accessToken]);
  useEffect(() => {
    if (tab !== 'todos' || !accessToken) return;
    void loadTodos(accessToken);
  }, [tab, accessToken, todoFilterDateFrom, todoFilterDateTo]);
  useEffect(() => {
    if (tab !== 'workouts') return;
    if (
      (workoutsSubTab === 'ai-trainer' || workoutsSubTab === 'weekly-reviews') &&
      showWorkoutAiTrainerTab
    ) {
      return;
    }
    if (
      workoutsSubTab === 'ai-trainer' ||
      workoutsSubTab === 'weekly-reviews'
    ) {
      openStrengthSubTab('my-workout');
    }
  }, [tab, workoutsSubTab, showWorkoutAiTrainerTab]);
  const habitFilterRange = useMemo(() => {
    const from =
      habitFilterDateFrom <= habitFilterDateTo
        ? habitFilterDateFrom
        : habitFilterDateTo;
    const to =
      habitFilterDateFrom <= habitFilterDateTo
        ? habitFilterDateTo
        : habitFilterDateFrom;
    return { from, to };
  }, [habitFilterDateFrom, habitFilterDateTo]);
  const habitCreatedIsoDate = (habit) => {
    const raw = habit?.createdAtUtc ?? habit?.CreatedAtUtc;
    if (!raw) return null;
    const d = new Date(raw);
    if (Number.isNaN(d.getTime())) return null;
    return d.toISOString().slice(0, 10);
  };
  const getHabitDisplayDates = (habit) => {
    const { from, to } = habitFilterRange;
    if (!from || !to) return [];
    const created = habitCreatedIsoDate(habit);
    const effectiveFrom = created && created > from ? created : from;
    if (effectiveFrom > to) return [];
    return getIsoDateRange(effectiveFrom, to);
  };

  const habitsActiveOverview = useMemo(
    () => habitsOverview.filter((h) => !isHabitMastered(h)),
    [habitsOverview],
  );
  const habitsMasteredOverview = useMemo(
    () => habitsOverview.filter((h) => isHabitMastered(h)),
    [habitsOverview],
  );

  const habitsByCategory = useMemo(() => {
    const groups = new Map();
    for (const h of habitsActiveOverview) {
      const key = h.categoryId ? String(h.categoryId) : '__none__';
      if (!groups.has(key)) {
        groups.set(key, {
          categoryId: h.categoryId || null,
          categoryName: h.categoryName || null,
          items: [],
        });
      }
      groups.get(key).items.push(h);
    }

    const categorySortOrder = (categoryId) => {
      const row = userCategories.find(
        (c) => String(c.id) === String(categoryId),
      );
      return row?.sortOrder ?? 9999;
    };

    return Array.from(groups.values())
      .map((g) => {
        const tabKey = g.categoryId ? String(g.categoryId) : '__none__';
        return {
          ...g,
          tabKey,
          showHeader: Boolean(g.categoryId && g.categoryName),
          tabLabel: g.categoryId && g.categoryName ? g.categoryName : null,
          doneCount: g.items.filter((x) => x.isDoneToday).length,
          totalCount: g.items.length,
        };
      })
      .sort((a, b) => {
        if (!a.categoryId) return 1;
        if (!b.categoryId) return -1;
        return categorySortOrder(a.categoryId) - categorySortOrder(b.categoryId);
      });
  }, [habitsActiveOverview, userCategories]);

  const todoStatusCounts = useMemo(() => {
    const open = todos.filter((t) => !t.doneDate).length;
    const done = todos.filter((t) => Boolean(t.doneDate)).length;
    return { all: todos.length, open, done };
  }, [todos]);

  const todosFilteredByStatus = useMemo(() => {
    if (todoStatusFilter === 'open') {
      return todos.filter((t) => !t.doneDate);
    }
    if (todoStatusFilter === 'done') {
      return todos.filter((t) => Boolean(t.doneDate));
    }
    return todos;
  }, [todos, todoStatusFilter]);

  const todosByCategory = useMemo(() => {
    const groups = new Map();
    for (const t of todosFilteredByStatus) {
      const key = t.categoryId ? String(t.categoryId) : '__none__';
      if (!groups.has(key)) {
        groups.set(key, {
          categoryId: t.categoryId || null,
          categoryName: t.categoryName || null,
          items: [],
        });
      }
      groups.get(key).items.push(t);
    }

    const categorySortOrder = (categoryId) => {
      const row = userCategories.find(
        (c) => String(c.id) === String(categoryId),
      );
      return row?.sortOrder ?? 9999;
    };

    return Array.from(groups.values())
      .map((g) => {
        const tabKey = g.categoryId ? String(g.categoryId) : '__none__';
        return {
          ...g,
          tabKey,
          showHeader: Boolean(g.categoryId && g.categoryName),
          tabLabel: g.categoryId && g.categoryName ? g.categoryName : null,
          doneCount: g.items.filter((x) => Boolean(x.doneDate)).length,
          totalCount: g.items.length,
        };
      })
      .sort((a, b) => {
        if (!a.categoryId) return 1;
        if (!b.categoryId) return -1;
        return categorySortOrder(a.categoryId) - categorySortOrder(b.categoryId);
      });
  }, [todosFilteredByStatus, userCategories]);

  const todosAllCategoryGroup = useMemo(() => {
    if (todosFilteredByStatus.length === 0) return null;
    return {
      tabKey: '__all__',
      categoryId: null,
      categoryName: null,
      tabLabel: 'Все',
      showHeader: false,
      showCategoryColumn: true,
      doneCount: todosFilteredByStatus.filter((x) => Boolean(x.doneDate))
        .length,
      totalCount: todosFilteredByStatus.length,
      items: todosFilteredByStatus,
    };
  }, [todosFilteredByStatus]);

  const todoStatusFilterOptions = useMemo(
    () => [
      { value: 'all', label: `Все (${todoStatusCounts.all})` },
      { value: 'open', label: `Открытые (${todoStatusCounts.open})` },
      { value: 'done', label: `Готово (${todoStatusCounts.done})` },
    ],
    [todoStatusCounts],
  );

  const habitsAllCategoryGroup = useMemo(() => {
    if (habitsActiveOverview.length === 0) return null;
    return {
      tabKey: '__all__',
      categoryId: null,
      categoryName: null,
      tabLabel: 'Все',
      showHeader: false,
      showCategoryColumn: true,
      doneCount: habitsActiveOverview.filter((x) => x.isDoneToday).length,
      totalCount: habitsActiveOverview.length,
      items: habitsActiveOverview,
    };
  }, [habitsActiveOverview]);

  const masteredHabitsInView = useMemo(() => {
    if (habitsMasteredOverview.length === 0) return [];
    if (habitsCategoryTabKey === '__all__') return habitsMasteredOverview;
    return habitsMasteredOverview.filter((h) => {
      const key = h.categoryId ? String(h.categoryId) : '__none__';
      return key === habitsCategoryTabKey;
    });
  }, [habitsMasteredOverview, habitsCategoryTabKey]);

  const masteredHabitsShowCategoryColumn = useMemo(
    () =>
      habitsCategoryTabKey === '__all__' ||
      masteredHabitsInView.some((h) => h.categoryId),
    [habitsCategoryTabKey, masteredHabitsInView],
  );

  const habitsCategoryFilterOptions = useMemo(() => {
    const options = [];
    if (habitsAllCategoryGroup) {
      options.push({
        value: '__all__',
        label: `Все (${habitsAllCategoryGroup.doneCount} / ${habitsAllCategoryGroup.totalCount})`,
      });
    }
    for (const group of habitsByCategory) {
      options.push({
        value: group.tabKey,
        label: `${group.tabLabel || 'Без категории'} (${group.doneCount} / ${group.totalCount})`,
      });
    }
    return options;
  }, [habitsAllCategoryGroup, habitsByCategory]);

  const todosCategoryFilterOptions = useMemo(() => {
    const options = [];
    if (todosAllCategoryGroup) {
      options.push({
        value: '__all__',
        label: `Все (${todosAllCategoryGroup.doneCount} / ${todosAllCategoryGroup.totalCount})`,
      });
    }
    for (const group of todosByCategory) {
      options.push({
        value: group.tabKey,
        label: `${group.tabLabel || 'Без категории'} (${group.doneCount} / ${group.totalCount})`,
      });
    }
    return options;
  }, [todosAllCategoryGroup, todosByCategory]);

  const activeHabitsCategoryGroup = useMemo(() => {
    if (habitsActiveOverview.length === 0) return null;
    if (habitsCategoryTabKey === '__all__') {
      return habitsAllCategoryGroup;
    }
    if (habitsByCategory.length === 0) {
      return habitsAllCategoryGroup;
    }
    return (
      habitsByCategory.find((g) => g.tabKey === habitsCategoryTabKey) ??
      habitsAllCategoryGroup
    );
  }, [
    habitsActiveOverview.length,
    habitsByCategory,
    habitsCategoryTabKey,
    habitsAllCategoryGroup,
  ]);

  useEffect(() => {
    if (tab !== 'habits' || !hasPermission(APP_PERMISSION.Habits) || !accessToken)
      return;
    const habits = [
      ...(activeHabitsCategoryGroup?.items ?? []),
      ...masteredHabitsInView,
    ];
    void loadHabitsCategoryCheckins(habits, accessToken);
  }, [
    tab,
    accessToken,
    habitsCategoryTabKey,
    habitFilterDateFrom,
    habitFilterDateTo,
    activeHabitsCategoryGroup,
    masteredHabitsInView,
  ]);

  const activeTodosCategoryGroup = useMemo(() => {
    if (todosFilteredByStatus.length === 0) return null;
    if (todosCategoryTabKey === '__all__') {
      return todosAllCategoryGroup;
    }
    if (todosByCategory.length === 0) {
      return todosAllCategoryGroup;
    }
    return (
      todosByCategory.find((g) => g.tabKey === todosCategoryTabKey) ??
      todosAllCategoryGroup
    );
  }, [todosFilteredByStatus.length, todosByCategory, todosCategoryTabKey, todosAllCategoryGroup]);

  const sortedActiveTodoItems = useMemo(() => {
    const items = activeTodosCategoryGroup?.items ?? [];
    if (!todoTableSort.key) return items;
    return [...items].sort((a, b) =>
      compareTodoItems(a, b, todoTableSort.key, todoTableSort.dir),
    );
  }, [activeTodosCategoryGroup, todoTableSort]);

  const habitPeriodAnalytics = useMemo(() => {
    const habits = activeHabitsCategoryGroup?.items ?? [];
    if (habits.length === 0) return null;
    const today = getTodayIsoDate();
    return computeHabitPeriodAnalytics({
      habits,
      checkinsByHabitId: habitsCheckinsByHabitId,
      filterRange: habitFilterRange,
      getDisplayDates: getHabitDisplayDates,
      resolveStatus: (statusMap, habit, date) =>
        resolveHabitStatusForDate(
          statusMap,
          habit,
          date,
          date === today ? 'todayStatus' : null,
        ),
    });
  }, [
    activeHabitsCategoryGroup,
    habitsCheckinsByHabitId,
    habitFilterRange,
    habitFilterDateFrom,
    habitFilterDateTo,
  ]);

  const todoPeriodAnalytics = useMemo(() => {
    const items = activeTodosCategoryGroup?.items ?? [];
    if (items.length === 0) return null;
    return computeTodoPeriodAnalytics({
      todos: items,
      filterFrom: todoFilterDateFrom,
      filterTo: todoFilterDateTo,
      getToday: getTodayIsoDate,
      getDateRange: getIsoDateRange,
    });
  }, [
    activeTodosCategoryGroup,
    todoFilterDateFrom,
    todoFilterDateTo,
    todosFilteredByStatus,
    todoStatusFilter,
    todosCategoryTabKey,
  ]);

  useEffect(() => {
    if (habitsActiveOverview.length === 0 && habitsMasteredOverview.length === 0) {
      setHabitsCategoryTabKey('__all__');
      return;
    }
    setHabitsCategoryTabKey((prev) => {
      if (prev === '__all__') return prev;
      const keys = habitsByCategory.map((g) => g.tabKey);
      return keys.includes(prev) ? prev : '__all__';
    });
  }, [habitsByCategory, habitsActiveOverview.length, habitsMasteredOverview.length]);
  useEffect(() => {
    if (todosFilteredByStatus.length === 0) {
      setTodosCategoryTabKey('__all__');
      return;
    }
    setTodosCategoryTabKey((prev) => {
      if (prev === '__all__') return prev;
      const keys = todosByCategory.map((g) => g.tabKey);
      return keys.includes(prev) ? prev : '__all__';
    });
  }, [todosByCategory, todosFilteredByStatus.length]);

  const selectedProgram = useMemo(
    () =>
      workoutPrograms.find((x) => x.sessionCode === selectedProgramCode) ||
      null,
    [workoutPrograms, selectedProgramCode],
  );
  const filteredWorkoutExerciseCatalog = useMemo(() => {
    const query = exerciseCatalogSearch.trim().toLowerCase();
    let items = workoutExerciseCatalog;
    if (query) {
      items = items.filter((exercise) =>
        String(exercise.name || '')
          .toLowerCase()
          .includes(query),
      );
    }
    if (exerciseCatalogSort.key === 'name') {
      const mult = exerciseCatalogSort.dir === 'asc' ? 1 : -1;
      items = [...items].sort(
        (a, b) =>
          mult *
          String(a.name ?? '').localeCompare(String(b.name ?? ''), 'ru'),
      );
    }
    return items;
  }, [workoutExerciseCatalog, exerciseCatalogSearch, exerciseCatalogSort]);
  const filteredProgramExerciseCatalog = useMemo(() => {
    const query = programExercisePickerSearch.trim().toLowerCase();
    if (!query) return workoutExerciseCatalog;
    return workoutExerciseCatalog.filter((exercise) =>
      String(exercise.name || '')
        .toLowerCase()
        .includes(query),
    );
  }, [workoutExerciseCatalog, programExercisePickerSearch]);
  const selectedCatalogExercise = useMemo(() => {
    const trimmed = programExercisePickerSearch.trim();
    if (!trimmed) return null;
    return (
      workoutExerciseCatalog.find(
        (x) =>
          String(x.name || '').trim().toLowerCase() === trimmed.toLowerCase(),
      ) || null
    );
  }, [workoutExerciseCatalog, programExercisePickerSearch]);
  const exerciseHistoryInsights = useMemo(
    () => buildExerciseHistoryInsights(workoutHistoryForStats),
    [workoutHistoryForStats],
  );
  const filteredWeightTrackerEntries = useMemo(
    () =>
      weightTrackerEntries.filter((row) => {
        const d = String(row.date || '').slice(0, 10);
        if (!d) return false;
        if (weightFilterDateFrom && d < weightFilterDateFrom) return false;
        if (weightFilterDateTo && d > weightFilterDateTo) return false;
        return true;
      }),
    [weightTrackerEntries, weightFilterDateFrom, weightFilterDateTo],
  );
  const weightChartData = useMemo(
    () =>
      [...filteredWeightTrackerEntries]
        .filter((x) => x?.date && Number.isFinite(Number(x?.weightKg)))
        .map((x) => ({ id: x.id, date: x.date, weightKg: Number(x.weightKg) }))
        .sort((a, b) => Date.parse(a.date) - Date.parse(b.date)),
    [filteredWeightTrackerEntries],
  );
  const weightChartBounds = useMemo(() => {
    if (weightChartData.length === 0) return null;
    const min = Math.min(...weightChartData.map((x) => x.weightKg));
    const max = Math.max(...weightChartData.map((x) => x.weightKg));
    if (min === max) {
      return { min: min - 1, max: max + 1 };
    }
    const pad = Math.max((max - min) * 0.1, 0.5);
    return { min: min - pad, max: max + pad };
  }, [weightChartData]);
  const weightChartPoints = useMemo(() => {
    if (!weightChartBounds || weightChartData.length === 0) return [];
    const chartW = 640;
    const chartH = 260;
    const left = 48;
    const right = 16;
    const top = 16;
    const bottom = 34;
    const innerW = chartW - left - right;
    const innerH = chartH - top - bottom;
    const count = weightChartData.length;
    return weightChartData.map((row, idx) => {
      const x =
        left + (count === 1 ? innerW / 2 : (idx / (count - 1)) * innerW);
      const yRatio =
        (row.weightKg - weightChartBounds.min) /
        (weightChartBounds.max - weightChartBounds.min);
      const y = top + (1 - yRatio) * innerH;
      return { ...row, x, y };
    });
  }, [weightChartData, weightChartBounds]);
  const weightChartPath = useMemo(
    () =>
      weightChartPoints
        .map(
          (p, idx) =>
            `${idx === 0 ? 'M' : 'L'} ${p.x.toFixed(2)} ${p.y.toFixed(2)}`,
        )
        .join(' '),
    [weightChartPoints],
  );
  const historyMaxSetCount = useMemo(
    () =>
      Math.max(
        0,
        ...(selectedWorkoutHistorySession?.exercises || []).map(
          (exercise) => (exercise.sets || []).length,
        ),
      ),
    [selectedWorkoutHistorySession],
  );

  const previewAssistantRow = useMemo(() => {
    if (!chatAssistantPreviewId) return null;
    return (
      aiAssistants.find(
        (x) => String(x.id) === String(chatAssistantPreviewId),
      ) ??
      adminAiAssistants.find(
        (x) => String(x.id) === String(chatAssistantPreviewId),
      ) ??
      null
    );
  }, [chatAssistantPreviewId, aiAssistants, adminAiAssistants]);

  return (
    <>
      {isApiLoading && (
        <div className="global-api-overlay" aria-hidden="true" />
      )}
      <div
        className={`global-api-loader${isApiLoading ? ' visible' : ''}`}
        aria-live="polite"
        aria-hidden={!isApiLoading}
      >
        <span className="spinner" aria-hidden="true" />
        <span>Загрузка данных...</span>
      </div>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route
          path="/login"
          element={
            <main className="auth-page">
              <section className="auth-card">
                <h1>Вход</h1>
                <p className="subtitle">Авторизуйся для доступа к приложению</p>
                <label>Логин</label>
                <input
                  value={username}
                  disabled={isLoginLoading}
                  onChange={(e) => {
                    setUsername(e.target.value);
                    if (loginError) setLoginError('');
                  }}
                />
                <label>Пароль</label>
                <input
                  value={password}
                  disabled={isLoginLoading}
                  onChange={(e) => {
                    setPassword(e.target.value);
                    if (loginError) setLoginError('');
                  }}
                  type="password"
                />
                <button
                  onClick={() => loginAndStore(navigate)}
                  disabled={isLoginLoading}
                  aria-busy={isLoginLoading}
                >
                  {isLoginLoading ? (
                    <span className="btn-loading">
                      <span className="spinner" aria-hidden="true" />
                      Входим...
                    </span>
                  ) : (
                    'Войти'
                  )}
                </button>
                {loginError && <p className="auth-error">{loginError}</p>}
                <p className="auth-link">
                  Нет аккаунта? <Link to="/register">Регистрация</Link>
                </p>
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
                <label>Логин</label>
                <input
                  value={registerUsername}
                  onChange={(e) => {
                    setRegisterUsername(e.target.value);
                    if (registerError) setRegisterError('');
                  }}
                />
                <label>Пароль</label>
                <input
                  value={registerPassword}
                  onChange={(e) => {
                    setRegisterPassword(e.target.value);
                    if (registerError) setRegisterError('');
                  }}
                  type="password"
                />
                <button
                  onClick={async () => {
                    setRegisterError('');
                    const result = await request('POST', '/auth/register', {
                      username: registerUsername.trim(),
                      password: registerPassword,
                    });
                    handleResult(result);
                    if (result.ok) {
                      navigate('/login');
                      return;
                    }

                    const message =
                      result.data?.message ||
                      (result.status === 409
                        ? 'Пользователь с таким логином уже существует.'
                        : '') ||
                      (result.status === 400
                        ? 'Проверь логин и пароль: данные не прошли валидацию.'
                        : '') ||
                      (result.status === 0
                        ? 'Сервер недоступен. Проверь подключение и адрес API.'
                        : '') ||
                      'Не удалось зарегистрироваться. Попробуй еще раз.';
                    setRegisterError(message);
                  }}
                >
                  Зарегистрироваться
                </button>
                {registerError && <p className="auth-error">{registerError}</p>}
                <p className="auth-link">
                  Уже есть аккаунт? <Link to="/login">Вход</Link>
                </p>
              </section>
            </main>
          }
        />
        <Route
          path="/app"
          element={
            !isLoggedIn ? (
              <Navigate to="/login" replace />
            ) : (
              <div className="dashboard-shell">
                <TopNav
                  tab={tab}
                  currentUserName={topbarDisplayName}
                  sidebarTabs={sidebarNavTabs}
                  accountMenuItems={accountNavItems}
                  onTabChange={(id) => {
                    if (id === 'admin' && !hasAdminAccess) return;
                    if (id === 'ai' && !hasAiAccess) return;
                    if (id === 'workouts' && !hasPermission(APP_PERMISSION.Workouts))
                      return;
                    if (id === 'bike' && !hasPermission(APP_PERMISSION.Bike))
                      return;
                    if (id === 'progress' && !hasPermission(APP_PERMISSION.Progress))
                      return;
                    if (id === 'habits' && !hasPermission(APP_PERMISSION.Habits))
                      return;
                    if (id === 'todos' && !hasPermission(APP_PERMISSION.Todos))
                      return;
                    if (
                      id === 'investments' &&
                      !hasPermission(APP_PERMISSION.Investments)
                    )
                      return;
                    if (id === 'profile' && !hasPermission(APP_PERMISSION.Profile))
                      return;
                    setTab(id);
                    if (id === 'profile') {
                      loadMyProfile();
                    }
                    if (id === 'progress') {
                      loadMyProfile();
                    }
                    if (id === 'habits' || id === 'todos') {
                      loadMyProfile();
                    }
                    if (id === 'workouts') {
                      loadWorkoutSessions();
                      loadWorkoutExerciseCatalog();
                    }
                    if (id === 'bike') {
                      loadBikeActivities();
                    }
                    if (id === 'investments') {
                      loadInvestments();
                    }
                    if (id === 'ai') {
                      loadAiAssistants(aiToken);
                    }
                    if (id === 'admin') {
                      loadUsers();
                      loadAdminDialogs();
                      loadAdminAiAssistants();
                      loadAdminCategories();
                      loadAdminRoles();
                      loadAdminPermissionsCatalog();
                      if (hasAiAccess) loadAiAssistants(aiToken);
                    }
                  }}
                  onLogout={() => {
                    setAccessToken('');
                    setAdminToken('');
                    setAiToken('');
                    setCurrentUserName('');
                    setCurrentUserRoles([]);
                    setCurrentUserPermissions([]);
                    setProfileBirthDate('');
                    setProfileHeightCm('');
                    setProfileWeightKg('');
                    setProfilePhone('');
                    setProfileCity('');
                    setProfileAbout('');
                    setProfileFirstName('');
                    setProfileLastName('');
                    setProfileAiSummary('');
                    setProfileTelegramLinked(false);
                    setIsTelegramLinkModalOpen(false);
                    setTelegramLinkUrl('');
                    setTelegramLinkExpiresAt('');
                    setTelegramLinkError('');
                    navigate('/login');
                  }}
                />

                <main className="app">
                {tab === 'ai' && hasAiAccess && (
                  <section className="card-grid">
                    <section className="card full-span">
                      <h3>ИИ помощники</h3>
                      {aiAssistants.length === 0 && (
                        <p className="subtitle">
                          Пока нет активных помощников. Администратор может
                          добавить их во вкладке «Админ».
                        </p>
                      )}
                      {aiAssistants.length > 0 && (
                        <ul
                          className="ai-assistant-list"
                          style={{ listStyle: 'none', padding: 0, margin: 0 }}
                        >
                          {aiAssistants.map((a) => (
                            <li
                              key={a.id}
                              style={{
                                display: 'flex',
                                alignItems: 'flex-start',
                                justifyContent: 'space-between',
                                gap: '12px',
                                padding: '10px 0',
                                borderBottom:
                                  '1px solid var(--border-subtle, rgba(255,255,255,0.08))',
                              }}
                            >
                              <div>
                                <strong>{a.name}</strong>
                                {a.description ? (
                                  <div
                                    className="subtitle"
                                    style={{ marginTop: 4 }}
                                  >
                                    {a.description}
                                  </div>
                                ) : null}
                              </div>
                              <div
                                style={{
                                  display: 'flex',
                                  flexDirection: 'column',
                                  gap: 8,
                                  alignItems: 'stretch',
                                  flexShrink: 0,
                                }}
                              >
                                <button
                                  type="button"
                                  className={a.selected ? '' : 'ghost-btn'}
                                  onClick={() =>
                                    toggleAiAssistantRow(a.id, a.selected)
                                  }
                                >
                                  {a.selected ? 'Выключить' : 'Включить'}
                                </button>
                                <button
                                  type="button"
                                  className="ghost-btn"
                                  onClick={() => openAiExtraInfoModal(a.id)}
                                  title="Заполнить дополнительные поля для этого помощника"
                                >
                                  Доп. поля
                                </button>
                              </div>
                            </li>
                          ))}
                        </ul>
                      )}

                      {aiAssistants.some((x) => x.selected) ? (
                        <div
                          className="ai-assistant-chat-panel"
                          style={{
                            marginTop: 24,
                            paddingTop: 20,
                            borderTop:
                              '1px solid var(--border-subtle, rgba(255,255,255,0.12))',
                          }}
                        >
                          <h4 style={{ marginTop: 0 }}>Чат с помощником</h4>
                          <p className="subtitle">
                            Сейчас ответы строятся с помощником «
                            <strong>
                              {aiAssistants.find((x) => x.selected)?.name}
                            </strong>
                            »: его системный промпт и заполненные для него доп.
                            поля передаются в модель вместе с историей диалога.
                          </p>
                          <p className="subtitle">
                            Можно создать отдельный диалог или писать без
                            выбранного диалога — тогда создастся новый.
                          </p>
                          <div className="row">
                            <select
                              value={currentDialogId}
                              onChange={(e) => {
                                setCurrentDialogId(e.target.value);
                                loadDialogMessages(e.target.value);
                              }}
                            >
                              <option value="">Нет диалогов</option>
                              {dialogOptions}
                            </select>
                            <button
                              type="button"
                              onClick={() => {
                                setAiDialogTitleDraft('');
                                setAiDialogModalKind('new');
                              }}
                            >
                              + Новый
                            </button>
                            <button
                              type="button"
                              onClick={() => {
                                if (!currentDialogId) return;
                                const current = dialogs.find(
                                  (d) => d.id === currentDialogId,
                                );
                                setAiDialogTitleDraft(
                                  current?.title || 'Новый диалог',
                                );
                                setAiDialogModalKind('rename');
                              }}
                              title="Переименовать"
                            >
                              ✏️
                            </button>
                            <button
                              type="button"
                              className="danger-btn danger-btn--icon"
                              onClick={() => {
                                if (!currentDialogId) return;
                                setAiDialogModalKind('delete');
                              }}
                              title="Удалить"
                            >
                              ×
                            </button>
                          </div>
                          <div className="chat-messages">
                            {chatMessages.map((m, i) => (
                              <div
                                key={i}
                                className={`chat-msg ${m.role === 'user' ? 'user' : 'assistant'}`}
                              >
                                {m.content}
                              </div>
                            ))}
                          </div>
                          <div className="row">
                            <input
                              value={chatPrompt}
                              onChange={(e) => setChatPrompt(e.target.value)}
                              placeholder="Введите сообщение..."
                            />
                            <button type="button" onClick={() => sendChat()}>
                              Отправить
                            </button>
                          </div>
                        </div>
                      ) : aiAssistants.length > 0 ? (
                        <p
                          className="subtitle"
                          style={{
                            marginTop: 24,
                            paddingTop: 20,
                            borderTop:
                              '1px solid var(--border-subtle, rgba(255,255,255,0.12))',
                          }}
                        >
                          Чат будет доступен после включения помощника кнопкой
                          «Включить» выше.
                        </p>
                      ) : null}
                    </section>
                  </section>
                )}

                {tab === 'workouts' && (
                  <section className="card-grid">
                    <section className="card full-span">
                      <h3>Тренировки</h3>
                      <p className="subtitle">
                        Управляй программами, своими упражнениями и фактическими
                        тренировками.
                      </p>
                      <SegmentTabs
                        variant="primary"
                        ariaLabel="Разделы тренировок"
                      >
                        {hasPermission(APP_PERMISSION.Workouts) ? (
                        <SegmentTab
                          active={workoutsSubTab === 'strength'}
                          onClick={() => setWorkoutsSubTab('strength')}
                        >
                          Силовые тренировки
                        </SegmentTab>
                        ) : null}
                        {showWorkoutAiTrainerTab ? (
                          <>
                            <SegmentTab
                              active={workoutsSubTab === 'weekly-reviews'}
                              onClick={() =>
                                setWorkoutsSubTab('weekly-reviews')
                              }
                            >
                              Недельные обзоры
                            </SegmentTab>
                            <SegmentTab
                              active={workoutsSubTab === 'ai-trainer'}
                              onClick={() => setWorkoutsSubTab('ai-trainer')}
                            >
                              ИИ тренер
                            </SegmentTab>
                          </>
                        ) : null}
                      </SegmentTabs>

                      {workoutsSubTab === 'strength' &&
                        hasPermission(APP_PERMISSION.Workouts) && (
                        <SubNavGroup
                          label="Силовые тренировки"
                          ariaLabel="Подразделы силовых тренировок"
                        >
                          <SubNavTab
                            level="secondary"
                            active={strengthSubTab === 'my-workout'}
                            onClick={() => setStrengthSubTab('my-workout')}
                          >
                            Моя тренировка
                          </SubNavTab>
                          <SubNavTab
                            level="secondary"
                            active={strengthSubTab === 'history'}
                            onClick={() => setStrengthSubTab('history')}
                          >
                            История
                          </SubNavTab>
                          <SubNavTab
                            level="secondary"
                            active={strengthSubTab === 'manage'}
                            onClick={() => setStrengthSubTab('manage')}
                          >
                            Управление тренировкой
                          </SubNavTab>
                        </SubNavGroup>
                      )}

                      {workoutsSubTab === 'weekly-reviews' &&
                        showWorkoutAiTrainerTab && (
                          <div
                            className="weekly-reviews-panel"
                            style={{
                              marginTop: 16,
                              paddingTop: 16,
                              borderTop:
                                '1px solid var(--border-subtle, rgba(255,255,255,0.12))',
                            }}
                          >
                            <h4 style={{ marginTop: 0 }}>Недельные обзоры</h4>
                            <p className="subtitle">
                              ИИ анализирует прошлую календарную неделю (пн–вс,
                              UTC), сохраняет текст в журнал. Чат не используется.
                            </p>
                            {(() => {
                              const period = getPreviousCalendarWeekUtc();
                              return (
                                <p className="subtitle" style={{ marginTop: 8 }}>
                                  Следующий обзор:{' '}
                                  <strong>
                                    {period.periodFrom} — {period.periodTo}
                                  </strong>
                                </p>
                              );
                            })()}
                            <div
                              className="row"
                              style={{
                                flexWrap: 'wrap',
                                gap: 8,
                                marginTop: 16,
                              }}
                            >
                              <button
                                type="button"
                                disabled={weeklyReviewGenerating}
                                onClick={requestPreviousWeekWeeklyReview}
                                title="Сформировать или открыть сохранённый обзор за прошлую неделю"
                              >
                                {weeklyReviewGenerating
                                  ? 'Формируем обзор…'
                                  : 'Обзор за прошлую неделю'}
                              </button>
                              <button
                                type="button"
                                className="ghost-btn"
                                disabled={weeklyReviewGenerating}
                                onClick={() =>
                                  loadWeeklyTrainingReviews(aiToken)
                                }
                              >
                                Обновить список
                              </button>
                            </div>

                            <h4 style={{ marginTop: 20 }}>Сохранённые обзоры</h4>
                            <div className="workout-list">
                              {weeklyTrainingReviews.length === 0 && (
                                <div className="workout-empty">
                                  Пока нет обзоров. Нажмите «Обзор за прошлую
                                  неделю».
                                </div>
                              )}
                              {weeklyTrainingReviews.map((review) => (
                                <article
                                  key={review.id}
                                  className="workout-item"
                                >
                                  <h4>
                                    {review.periodFrom} — {review.periodTo}
                                  </h4>
                                  <div className="workout-meta">
                                    <span>
                                      {review.days === 7
                                        ? 'Неделя'
                                        : `${review.days} дн.`}
                                    </span>
                                    <span>
                                      Обновл.:{' '}
                                      {formatReviewDateUtc(
                                        review.updatedAtUtc,
                                      )}
                                    </span>
                                  </div>
                                  {review.preview ? (
                                    <p
                                      className="subtitle"
                                      style={{ marginTop: 8, marginBottom: 8 }}
                                    >
                                      {review.preview}
                                    </p>
                                  ) : null}
                                  <div className="row">
                                    <button
                                      type="button"
                                      className="ghost-btn"
                                      onClick={() =>
                                        openWeeklyReviewModal(review.id)
                                      }
                                    >
                                      Читать полностью
                                    </button>
                                    <button
                                      type="button"
                                      className="danger-btn danger-btn--icon"
                                      onClick={() =>
                                        openDeleteWeeklyReviewModal(review)
                                      }
                                      title="Удалить обзор"
                                    >
                                      ×
                                    </button>
                                  </div>
                                </article>
                              ))}
                            </div>

                            <ModalShell
                              open={isWeeklyReviewModalOpen}
                              onClose={closeWeeklyReviewModal}
                              scroll
                              wide
                            >
                              {weeklyReviewModalLoading ? (
                                <p className="subtitle">Загрузка обзора…</p>
                              ) : selectedWeeklyReview ? (
                                <>
                                  <h3 style={{ marginTop: 0 }}>
                                    Обзор {selectedWeeklyReview.periodFrom} —{' '}
                                    {selectedWeeklyReview.periodTo}
                                  </h3>
                                  <p className="subtitle">
                                    {selectedWeeklyReview.days === 7
                                      ? 'Период: 7 дней'
                                      : `Период: ${selectedWeeklyReview.days} дн.`}
                                    {' · '}
                                    Обновл.:{' '}
                                    {formatReviewDateUtc(
                                      selectedWeeklyReview.updatedAtUtc,
                                    )}
                                  </p>
                                  <div
                                    className="chat-msg assistant"
                                    style={{
                                      marginTop: 16,
                                      maxHeight: 'none',
                                      whiteSpace: 'pre-wrap',
                                    }}
                                  >
                                    {selectedWeeklyReview.content}
                                  </div>
                                  <div className="row" style={{ marginTop: 16 }}>
                                    <button
                                      type="button"
                                      className="ghost-btn"
                                      onClick={closeWeeklyReviewModal}
                                    >
                                      Закрыть
                                    </button>
                                    <button
                                      type="button"
                                      className="danger-btn danger-btn--icon"
                                      onClick={() =>
                                        openDeleteWeeklyReviewModal(
                                          selectedWeeklyReview,
                                        )
                                      }
                                      title="Удалить обзор"
                                    >
                                      ×
                                    </button>
                                  </div>
                                </>
                              ) : null}
                            </ModalShell>

                            <ModalShell
                              open={Boolean(pendingDeleteWeeklyReview)}
                              onClose={() => setPendingDeleteWeeklyReview(null)}
                            >
                              <h3>Удалить обзор</h3>
                              <p className="subtitle">
                                Удалить обзор за период{' '}
                                <strong>
                                  {pendingDeleteWeeklyReview?.periodFrom} —{' '}
                                  {pendingDeleteWeeklyReview?.periodTo}
                                </strong>
                                ? Это действие нельзя отменить.
                              </p>
                              <div className="row">
                                <button
                                  type="button"
                                  className="danger-btn danger-btn--icon"
                                  onClick={() => void confirmDeleteWeeklyReview()}
                                  title="Удалить"
                                >
                                  ×
                                </button>
                                <button
                                  type="button"
                                  className="ghost-btn"
                                  onClick={() =>
                                    setPendingDeleteWeeklyReview(null)
                                  }
                                >
                                  Отмена
                                </button>
                              </div>
                            </ModalShell>
                          </div>
                        )}

                      {workoutsSubTab === 'ai-trainer' &&
                        showWorkoutAiTrainerTab && (
                          <div
                            className="ai-assistant-chat-panel"
                            style={{
                              marginTop: 16,
                              paddingTop: 16,
                              borderTop:
                                '1px solid var(--border-subtle, rgba(255,255,255,0.12))',
                            }}
                          >
                            <h4 style={{ marginTop: 0 }}>ИИ тренер — чат</h4>
                            <p className="subtitle">
                              Свободный диалог с помощником «Тренер». Недельные
                              обзоры — во вкладке «Недельные обзоры».
                            </p>
                            <div className="row">
                              <select
                                value={currentDialogId}
                                onChange={(e) => {
                                  setCurrentDialogId(e.target.value);
                                  loadDialogMessages(e.target.value);
                                }}
                              >
                                <option value="">Нет диалогов</option>
                                {dialogOptions}
                              </select>
                              <button
                                type="button"
                                onClick={() => {
                                  setAiDialogTitleDraft('');
                                  setAiDialogModalKind('new');
                                }}
                              >
                                + Новый
                              </button>
                              <button
                                type="button"
                                onClick={() => {
                                  if (!currentDialogId) return;
                                  const current = dialogs.find(
                                    (d) => d.id === currentDialogId,
                                  );
                                  setAiDialogTitleDraft(
                                    current?.title || 'Новый диалог',
                                  );
                                  setAiDialogModalKind('rename');
                                }}
                                title="Переименовать"
                              >
                                ✏️
                              </button>
                              <button
                                type="button"
                                className="danger-btn danger-btn--icon"
                                onClick={() => {
                                  if (!currentDialogId) return;
                                  setAiDialogModalKind('delete');
                                }}
                                title="Удалить"
                              >
                                ×
                              </button>
                            </div>
                            <div className="chat-messages">
                              {chatMessages.map((m, i) => (
                                <div
                                  key={i}
                                  className={`chat-msg ${m.role === 'user' ? 'user' : 'assistant'}`}
                                >
                                  {m.content}
                                </div>
                              ))}
                            </div>
                            <div className="row">
                              <input
                                value={chatPrompt}
                                onChange={(e) => setChatPrompt(e.target.value)}
                                placeholder="Сообщение ИИ тренеру…"
                              />
                              <button type="button" onClick={() => sendChat()}>
                                Отправить
                              </button>
                            </div>
                          </div>
                        )}

                      {workoutsSubTab === 'strength' &&
                        hasPermission(APP_PERMISSION.Workouts) &&
                        strengthSubTab === 'manage' && (
                          <SubNavGroup
                            label="Управление тренировкой"
                            level="tertiary"
                            nested
                            ariaLabel="Управление тренировкой"
                          >
                            <SubNavTab
                              level="tertiary"
                              active={workoutsManageSubTab === 'add'}
                              onClick={() => setWorkoutsManageSubTab('add')}
                            >
                              Программы
                            </SubNavTab>
                            <SubNavTab
                              level="tertiary"
                              active={workoutsManageSubTab === 'exercises'}
                              onClick={() =>
                                setWorkoutsManageSubTab('exercises')
                              }
                            >
                              Упражнения
                            </SubNavTab>
                          </SubNavGroup>
                        )}

                      {workoutsSubTab === 'strength' &&
                        hasPermission(APP_PERMISSION.Workouts) &&
                        strengthSubTab === 'manage' &&
                        workoutsManageSubTab === 'add' && (
                          <>
                            <div className="row">
                              <button onClick={openProgramCreateModal}>
                                Новая программа
                              </button>
                              <button
                                className="ghost-btn"
                                onClick={openPlanningImportDialog}
                              >
                                Импорт планирования JSON
                              </button>
                              <button
                                className="ghost-btn"
                                onClick={downloadPlanningJsonTemplate}
                              >
                                Скачать шаблон планирования
                              </button>
                            </div>
                            <input
                              ref={planningImportInputRef}
                              type="file"
                              accept=".json,application/json"
                              onChange={handlePlanningJsonImportChange}
                              style={{ display: 'none' }}
                            />

                            <h4>Сохраненные программы</h4>
                            <div className="workout-list">
                              {workoutPrograms.length === 0 && (
                                <div className="workout-empty">
                                  Пока нет программ.
                                </div>
                              )}
                              {workoutPrograms.map((session) => (
                                <article
                                  key={session.id}
                                  className="workout-item"
                                >
                                  <h4>{session.day}</h4>
                                  <div className="workout-meta">
                                    <span>Код: {session.sessionCode}</span>
                                    <span>
                                      Упражнений: {session.exercises.length}
                                    </span>
                                  </div>
                                  <div className="row">
                                    <button
                                      onClick={() =>
                                        openProgramEditModal(session)
                                      }
                                      title="Редактировать"
                                    >
                                      ✏️
                                    </button>
                                    <button
                                      className="ghost-btn"
                                      onClick={() => {
                                        setSelectedProgramCode(
                                          session.sessionCode,
                                        );
                                        openStrengthSubTab('my-workout');
                                      }}
                                    >
                                      Начать тренировку
                                    </button>
                                    <button
                                      className="danger-btn danger-btn--icon"
                                      onClick={() =>
                                        openProgramDeleteModal(session)
                                      }
                                      title="Удалить"
                                    >
                                      ×
                                    </button>
                                  </div>
                                </article>
                              ))}
                            </div>
                          </>
                        )}

                      {workoutsSubTab === 'strength' &&
                        hasPermission(APP_PERMISSION.Workouts) &&
                        strengthSubTab === 'manage' &&
                        workoutsManageSubTab === 'exercises' && (
                          <>
                            <p className="subtitle">
                              Создай новое упражнение и используй его в
                              программах и тренировках.
                            </p>
                            <div className="row">
                              <button onClick={openCreateCatalogExerciseModal}>
                                Создать упражнение
                              </button>
                              <button
                                className="ghost-btn"
                                onClick={openExercisesImportDialog}
                              >
                                Загрузить упражнения JSON
                              </button>
                              <button
                                className="ghost-btn"
                                onClick={downloadExercisesJsonTemplate}
                              >
                                Скачать шаблон упражнений
                              </button>
                              <button
                                className="ghost-btn"
                                onClick={exportExercisesIdNameJson}
                              >
                                Выгрузить ID + название
                              </button>
                            </div>
                            <input
                              ref={exercisesImportInputRef}
                              type="file"
                              accept=".json,application/json"
                              onChange={handleExercisesJsonImportChange}
                              style={{ display: 'none' }}
                            />
                            <div className="row filter-toolbar">
                              <label>
                                Поиск
                                <input
                                  type="search"
                                  value={exerciseCatalogSearch}
                                  onChange={(e) =>
                                    setExerciseCatalogSearch(e.target.value)
                                  }
                                  placeholder="По названию упражнения"
                                />
                              </label>
                            </div>
                            <div className="users-table-wrap">
                              <table className="users-table">
                                <thead>
                                  <tr>
                                    <th aria-sort={exerciseCatalogSortAria('name')}>
                                      <button
                                        type="button"
                                        className="table-sort-btn"
                                        onClick={() =>
                                          cycleExerciseCatalogSort('name')
                                        }
                                      >
                                        Упражнение
                                        <span
                                          className="table-sort-btn__icon"
                                          aria-hidden="true"
                                        >
                                          {exerciseCatalogSort.key === 'name'
                                            ? exerciseCatalogSort.dir === 'asc'
                                              ? '↑'
                                              : '↓'
                                            : '↕'}
                                        </span>
                                      </button>
                                    </th>
                                    <th>Группа мышц</th>
                                    <th>Комментарий</th>
                                    <th>Действия</th>
                                  </tr>
                                </thead>
                                <tbody>
                                  {workoutExerciseCatalog.length === 0 && (
                                    <tr>
                                      <td colSpan="4">
                                        Пока нет упражнений в базе.
                                      </td>
                                    </tr>
                                  )}
                                  {workoutExerciseCatalog.length > 0 &&
                                    filteredWorkoutExerciseCatalog.length ===
                                      0 && (
                                      <tr>
                                        <td colSpan="4">
                                          Ничего не найдено по запросу «
                                          {exerciseCatalogSearch.trim()}».
                                        </td>
                                      </tr>
                                    )}
                                  {filteredWorkoutExerciseCatalog.map(
                                    (exercise) => {
                                      const parsedMeta = parseProgramExerciseMeta(
                                        exercise.meta,
                                      );
                                      const comment = parsedMeta.isStructured
                                        ? parsedMeta.comment
                                        : parsedMeta.legacy;
                                      return (
                                        <tr key={exercise.id}>
                                          <td>{exercise.name}</td>
                                          <td>{parsedMeta.muscleGroup || '-'}</td>
                                          <td>{comment || '-'}</td>
                                          <td>
                                            <div className="todo-row-actions">
                                              <button
                                                type="button"
                                                onClick={() =>
                                                  openEditCatalogExerciseModal(
                                                    exercise,
                                                  )
                                                }
                                                title="Редактировать"
                                              >
                                                ✏️
                                              </button>
                                              <button
                                                className="danger-btn danger-btn--icon"
                                                onClick={() =>
                                                  openDeleteCatalogExerciseModal(
                                                    exercise.id,
                                                  )
                                                }
                                                title="Удалить"
                                              >
                                                ×
                                              </button>
                                            </div>
                                          </td>
                                        </tr>
                                      );
                                    },
                                  )}
                                </tbody>
                              </table>
                            </div>
                          </>
                        )}

                      {workoutsSubTab === 'strength' &&
                        hasPermission(APP_PERMISSION.Workouts) &&
                        strengthSubTab === 'my-workout' && (
                          <>
                            <label>Выбери программу</label>
                            <select
                              value={selectedProgramCode}
                              onChange={(e) =>
                                setSelectedProgramCode(e.target.value)
                              }
                            >
                              <option value="">Нет выбранной программы</option>
                              {workoutPrograms.map((program) => (
                                <option
                                  key={program.id}
                                  value={program.sessionCode}
                                >
                                  {program.day}
                                </option>
                              ))}
                            </select>
                            <div className="row">
                              <button
                                disabled={!selectedProgram}
                                onClick={() =>
                                  selectedProgram &&
                                  startWorkoutFromProgram(selectedProgram)
                                }
                              >
                                Начать тренировку
                              </button>
                              <button
                                className="ghost-btn"
                                onClick={createWorkoutFromScratch}
                              >
                                Создать с нуля
                              </button>
                            </div>
                            <p className="subtitle">
                              Заполнение и сохранение тренировки открывается в
                              окне поверх страницы. Завершенные тренировки
                              смотри во вкладке «История тренировок».
                            </p>
                            {(activeWorkoutSession || currentWorkout) && (
                              <div className="active-workout-banner">
                                <div>
                                  <span className="active-workout-badge">
                                    Активная
                                  </span>
                                  <span className="active-workout-title">
                                    {currentWorkout?.day ||
                                      activeWorkoutSession?.day ||
                                      'Тренировка'}
                                  </span>
                                  {(currentWorkout?.date ||
                                    activeWorkoutSession?.date ||
                                    activeWorkoutSession?._date) && (
                                    <span className="active-workout-date">
                                      {formatWorkoutDateLabel(
                                        currentWorkout?.date ||
                                          activeWorkoutSession?.date ||
                                          activeWorkoutSession?._date,
                                      )}
                                    </span>
                                  )}
                                </div>
                                <button
                                  type="button"
                                  onClick={openOrResumeWorkoutModal}
                                >
                                  Открыть тренировку
                                </button>
                              </div>
                            )}
                          </>
                        )}
                      {workoutsSubTab === 'strength' &&
                        hasPermission(APP_PERMISSION.Workouts) &&
                        strengthSubTab === 'history' && (
                          <>
                            <h4>История моих тренировок</h4>
                            <DatePeriodFilter
                              preset={historyDatePeriodPreset}
                              onPresetChange={setHistoryDatePeriodPreset}
                              from={historyDateFrom}
                              to={historyDateTo}
                              onFromChange={setHistoryDateFrom}
                              onToChange={setHistoryDateTo}
                              options={STANDARD_DATE_PERIOD_OPTIONS}
                              onApplyPreset={(v) =>
                                applyHistoryDatePreset(Number(v))
                              }
                            />
                            <div className="row">
                              <button
                                type="button"
                                className="ghost-btn"
                                onClick={exportHistorySelectionJson}
                                disabled={historyWorkoutLogs.length === 0}
                              >
                                Выгрузить JSON (вся выборка)
                              </button>
                            </div>
                            <div className="workout-list">
                              {historyWorkoutLogs.length === 0 && (
                                <div className="workout-empty">
                                  Нет тренировок по выбранному фильтру.
                                </div>
                              )}
                              {historyWorkoutLogs.map((session) => (
                                <article
                                  key={session.id}
                                  className="workout-item"
                                >
                                  <h4>{session.day}</h4>
                                  <div className="workout-meta">
                                    <span>
                                      Дата:{' '}
                                      {formatWorkoutDateLabel(
                                        session.date || session._date,
                                      ) || '-'}
                                    </span>
                                    <span>
                                      Упражнений: {session.exercises.length}
                                    </span>
                                  </div>
                                  <div className="row">
                                    <button
                                      className="ghost-btn"
                                      onClick={() =>
                                        openWorkoutHistoryModal(session)
                                      }
                                    >
                                      Подробнее
                                    </button>
                                    <button
                                      className="ghost-btn"
                                      onClick={() =>
                                        exportHistorySessionJson(session)
                                      }
                                    >
                                      JSON
                                    </button>
                                    <button
                                      className="danger-btn danger-btn--icon"
                                      onClick={() =>
                                        openDeleteWorkoutLogModal(session.id)
                                      }
                                      title="Удалить"
                                    >
                                      ×
                                    </button>
                                  </div>
                                </article>
                              ))}
                            </div>
                          </>
                        )}
                    </section>
                  </section>
                )}

                {tab === 'bike' && hasPermission(APP_PERMISSION.Bike) && (
                  <section className="card-grid">
                    <section className="card full-span">
                      <h3>Велотренировки</h3>
                      <p className="subtitle">
                        Импорт TCX (например из Zepp) и история поездок.
                      </p>
                      <>
                        <h4>Велотренировки (TCX)</h4>
                        <p className="subtitle">
                          Загрузите экспорт TCX (например из Zepp).
                          Принимаются только поездки со спортом{' '}
                          <strong>Biking</strong>. Файл на сервере не хранится
                          — сохраняются разобранные данные.
                        </p>
                        <div className="row">
                          <button
                            type="button"
                            onClick={() => bikeTcxImportRef.current?.click()}
                          >
                            Загрузить TCX
                          </button>
                          <input
                            ref={bikeTcxImportRef}
                            type="file"
                            accept=".tcx,application/xml,text/xml"
                            style={{ display: 'none' }}
                            onChange={(e) => {
                              const f = e.target.files?.[0];
                              if (f) importBikeTcxFile(f);
                            }}
                          />
                        </div>
                        {bikeImportMessage ? (
                          <p
                            className="subtitle"
                            style={{ whiteSpace: 'pre-wrap' }}
                          >
                            {bikeImportMessage}
                          </p>
                        ) : null}
                        <DatePeriodFilter
                          preset={bikeDatePeriodPreset}
                          onPresetChange={setBikeDatePeriodPreset}
                          from={bikeDateFrom}
                          to={bikeDateTo}
                          onFromChange={setBikeDateFrom}
                          onToChange={setBikeDateTo}
                          options={STANDARD_DATE_PERIOD_OPTIONS}
                          onApplyPreset={(v) =>
                            applyBikeDatePreset(Number(v))
                          }
                        />
                        <div className="users-table-wrap">
                          <table className="users-table">
                            <thead>
                              <tr>
                                <th>Старт</th>
                                <th>Название</th>
                                <th>Длительность</th>
                                <th>Дистанция, км</th>
                                <th>Ккал</th>
                                <th>ЧСС ср. / макс</th>
                                <th>Точек трека</th>
                                <th>Импорт</th>
                                <th />
                              </tr>
                            </thead>
                            <tbody>
                              {bikeActivities.length === 0 && (
                                <tr>
                                  <td colSpan="9">
                                    Нет записей по выбранному периоду.
                                  </td>
                                </tr>
                              )}
                              {bikeActivities.map((row) => (
                                <tr key={row.id}>
                                  <td>
                                    {formatUtcDateTime(row.startTimeUtc)}
                                  </td>
                                  <td>{row.notes || '—'}</td>
                                  <td>
                                    {formatBikeDurationSeconds(
                                      row.totalSeconds,
                                    )}
                                  </td>
                                  <td>
                                    {row.distanceMeters != null
                                      ? (
                                          Number(row.distanceMeters) / 1000
                                        ).toFixed(2)
                                      : '—'}
                                  </td>
                                  <td>
                                    {row.calories != null
                                      ? Math.round(row.calories)
                                      : '—'}
                                  </td>
                                  <td>
                                    {row.averageHeartRateBpm ?? '—'} /{' '}
                                    {row.maxHeartRateBpm ?? '—'}
                                  </td>
                                  <td>{row.trackpointCount}</td>
                                  <td>
                                    {formatUtcDateTime(row.importedAtUtc)}
                                  </td>
                                  <td>
                                    <button
                                      type="button"
                                      className="ghost-btn"
                                      onClick={() =>
                                        openBikeActivityDetail(row.id)
                                      }
                                    >
                                      Маршрут
                                    </button>{' '}
                                    <button
                                      type="button"
                                      className="danger-btn danger-btn--icon"
                                      onClick={() =>
                                        setPendingDeleteBikeActivityId(row.id)
                                      }
                                      title="Удалить"
                                    >
                                      ×
                                    </button>
                                  </td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      </>
                    </section>
                  </section>
                )}

                {tab === 'profile' && (
                  <section className="card-grid">
                    <section className="card full-span">
                      <h3>Мой профиль</h3>
                      <p className="subtitle">
                        Все поля необязательные. Изменения вносятся в отдельном
                        окне.
                      </p>
                      <div className="profile-readonly">
                        <div className="profile-readonly-grid">
                          <div className="profile-readonly-item">
                            <span className="profile-field-label">Имя</span>
                            <div className="profile-field-value">
                              {profileFirstName || '—'}
                            </div>
                          </div>
                          <div className="profile-readonly-item">
                            <span className="profile-field-label">Фамилия</span>
                            <div className="profile-field-value">
                              {profileLastName || '—'}
                            </div>
                          </div>
                          <div className="profile-readonly-item">
                            <span className="profile-field-label">
                              Дата рождения
                            </span>
                            <div className="profile-field-value">
                              {profileBirthDate || '—'}
                            </div>
                          </div>
                          <div className="profile-readonly-item">
                            <span className="profile-field-label">
                              Рост (см)
                            </span>
                            <div className="profile-field-value">
                              {profileHeightCm || '—'}
                            </div>
                          </div>
                          <div className="profile-readonly-item">
                            <span className="profile-field-label">
                              Вес (кг)
                            </span>
                            <div className="profile-field-value">
                              {profileWeightKg || '—'}
                            </div>
                          </div>
                          <div className="profile-readonly-item">
                            <span className="profile-field-label">Телефон</span>
                            <div className="profile-field-value">
                              {profilePhone || '—'}
                            </div>
                          </div>
                          <div className="profile-readonly-item">
                            <span className="profile-field-label">Город</span>
                            <div className="profile-field-value">
                              {profileCity || '—'}
                            </div>
                          </div>
                        </div>
                        <div className="profile-readonly-item">
                          <span className="profile-field-label">О себе</span>
                          <div className="profile-field-value">
                            {profileAbout || '—'}
                          </div>
                        </div>
                        <div className="profile-readonly-item profile-ai-summary-block">
                          <span className="profile-field-label">
                            Саммари от ИИ
                          </span>
                          <div className="profile-field-value profile-ai-summary-text">
                            {profileAiSummary ||
                              'Появится после ответов ассистента в чате.'}
                          </div>
                        </div>
                        <div className="profile-readonly-item">
                          <span className="profile-field-label">Telegram</span>
                          <div className="profile-field-value">
                            {profileTelegramLinked
                              ? 'Подключён — вес из бота сохраняется в дневник на вкладке «Прогресс».'
                              : 'Не подключён — можно вести вес только в приложении.'}
                          </div>
                          <div className="row" style={{ marginTop: 8 }}>
                            <button
                              type="button"
                              className="ghost-btn"
                              onClick={openTelegramLinkModal}
                            >
                              {profileTelegramLinked
                                ? 'Управление Telegram…'
                                : 'Подключить Telegram…'}
                            </button>
                          </div>
                        </div>
                      </div>
                      <div className="row">
                        <button
                          onClick={() => setIsProfileEditModalOpen(true)}
                          title="Редактировать профиль"
                        >
                          ✏️
                        </button>
                        <button
                          className="ghost-btn"
                          onClick={() => loadMyProfile()}
                        >
                          Обновить с сервера
                        </button>
                      </div>
                    </section>
                  </section>
                )}

                {tab === 'progress' && hasPermission(APP_PERMISSION.Progress) && (
                  <section className="card-grid">
                    <section className="card full-span">
                      <h3>Мой прогресс</h3>
                      <p className="subtitle">
                        История веса с синхронизацией профиля и AI-тренера.
                      </p>
                          <div className="row profile-row">
                            <div>
                              <label>Дата</label>
                              <input
                                type="date"
                                value={weightTrackerDate}
                                onChange={(e) =>
                                  setWeightTrackerDate(e.target.value)
                                }
                              />
                            </div>
                            <div>
                              <label>Вес (кг)</label>
                              <input
                                type="number"
                                step="0.1"
                                min="0"
                                value={weightTrackerValue}
                                onChange={(e) =>
                                  setWeightTrackerValue(e.target.value)
                                }
                                placeholder="Например: 78.4"
                              />
                            </div>
                          </div>
                          <div className="row">
                            <button
                              type="button"
                              onClick={submitWeightTrackerEntry}
                            >
                              Сохранить вес
                            </button>
                          </div>
                          <h4 style={{ margin: '12px 0 8px', fontSize: 14 }}>
                            Период на графике и в таблице
                          </h4>
                          <DatePeriodFilter
                            preset={weightDatePeriodPreset}
                            onPresetChange={setWeightDatePeriodPreset}
                            from={weightFilterDateFrom}
                            to={weightFilterDateTo}
                            onFromChange={setWeightFilterDateFrom}
                            onToChange={setWeightFilterDateTo}
                            options={STANDARD_DATE_PERIOD_OPTIONS}
                            onApplyPreset={(v) =>
                              applyWeightDatePreset(Number(v))
                            }
                          />
                          <div className="row">
                            <button
                              type="button"
                              className="ghost-btn"
                              onClick={exportWeightSelectionJson}
                              disabled={
                                filteredWeightTrackerEntries.length === 0
                              }
                            >
                              Выгрузить JSON (вся выборка)
                            </button>
                          </div>
                          <div
                            className="users-table-wrap"
                            style={{ marginTop: 8 }}
                          >
                            <h4 style={{ margin: '0 0 8px', fontSize: 14 }}>
                              График веса
                            </h4>
                            {weightTrackerEntries.length === 0 ? (
                              <div className="workout-empty">
                                Добавь минимум одну запись веса, чтобы увидеть
                                график.
                              </div>
                            ) : weightChartPoints.length === 0 ? (
                              <div className="workout-empty">
                                Нет записей по выбранному фильтру.
                              </div>
                            ) : (
                              <svg
                                viewBox="0 0 640 260"
                                width="100%"
                                height="260"
                                role="img"
                                aria-label="График изменения веса"
                              >
                                <line
                                  x1="48"
                                  y1="16"
                                  x2="48"
                                  y2="226"
                                  stroke="rgba(157,194,169,0.35)"
                                />
                                <line
                                  x1="48"
                                  y1="226"
                                  x2="624"
                                  y2="226"
                                  stroke="rgba(157,194,169,0.35)"
                                />
                                {weightChartPath ? (
                                  <path
                                    d={weightChartPath}
                                    fill="none"
                                    stroke="#3fb950"
                                    strokeWidth="3"
                                    strokeLinecap="round"
                                  />
                                ) : null}
                                {weightChartPoints.map((p, pi) => (
                                  <g
                                    key={`weight-point-${p.id ?? p.date}-${pi}`}
                                  >
                                    <circle
                                      cx={p.x}
                                      cy={p.y}
                                      r="4"
                                      fill="#3fb950"
                                      stroke="#0d1117"
                                      strokeWidth="1.5"
                                    />
                                    <title>{`${p.date}: ${p.weightKg} кг`}</title>
                                  </g>
                                ))}
                                <text
                                  x="8"
                                  y="20"
                                  fill="var(--muted)"
                                  fontSize="12"
                                >
                                  {weightChartBounds?.max.toFixed(1)} кг
                                </text>
                                <text
                                  x="8"
                                  y="230"
                                  fill="var(--muted)"
                                  fontSize="12"
                                >
                                  {weightChartBounds?.min.toFixed(1)} кг
                                </text>
                                {weightChartPoints[0] ? (
                                  <text
                                    x={weightChartPoints[0].x}
                                    y="248"
                                    textAnchor="start"
                                    fill="var(--muted)"
                                    fontSize="12"
                                  >
                                    {weightChartPoints[0].date}
                                  </text>
                                ) : null}
                                {weightChartPoints[
                                  weightChartPoints.length - 1
                                ] ? (
                                  <text
                                    x={
                                      weightChartPoints[
                                        weightChartPoints.length - 1
                                      ].x
                                    }
                                    y="248"
                                    textAnchor="end"
                                    fill="var(--muted)"
                                    fontSize="12"
                                  >
                                    {
                                      weightChartPoints[
                                        weightChartPoints.length - 1
                                      ].date
                                    }
                                  </text>
                                ) : null}
                              </svg>
                            )}
                          </div>
                          <div
                            className="users-table-wrap"
                            style={{ marginTop: 8 }}
                          >
                            <table className="users-table">
                              <thead>
                                <tr>
                                  <th>Дата</th>
                                  <th>Вес (кг)</th>
                                  <th aria-label="Удалить" />
                                </tr>
                              </thead>
                              <tbody>
                                {weightTrackerEntries.length === 0 && (
                                  <tr>
                                    <td colSpan="3">Записей пока нет.</td>
                                  </tr>
                                )}
                                {weightTrackerEntries.length > 0 &&
                                  filteredWeightTrackerEntries.length === 0 && (
                                    <tr>
                                      <td colSpan="3">
                                        Нет записей по выбранному фильтру.
                                      </td>
                                    </tr>
                                  )}
                                {filteredWeightTrackerEntries.map((row) => (
                                  <tr key={row.id}>
                                    <td>{row.date || '-'}</td>
                                    <td>{row.weightKg ?? '-'}</td>
                                    <td style={{ width: 52 }}>
                                      <button
                                        type="button"
                                        className="danger-btn danger-btn--icon"
                                        title="Удалить запись"
                                        onClick={() =>
                                          setPendingDeleteWeightEntry({
                                            id: row.id,
                                            date: row.date,
                                            weightKg: row.weightKg,
                                          })
                                        }
                                      >
                                        ×
                                      </button>
                                    </td>
                                  </tr>
                                ))}
                              </tbody>
                            </table>
                          </div>
                    </section>
                  </section>
                )}

                {tab === 'habits' && hasPermission(APP_PERMISSION.Habits) && (
                  <section className="card-grid">
                    <section className="card full-span">
                      <h3>Привычки</h3>
                      <>
                          <p className="subtitle" style={{ marginTop: 8 }}>
                            Отмечайте сегодня и вчера по клику. Остальной период
                            — только просмотр.
                          </p>
                          <div className="row">
                            <button
                              type="button"
                              onClick={() => {
                                setHabitsNewName('');
                                setHabitsNewCategoryId('');
                                setHabitsNewDaysToMaster('21');
                                setIsCreateHabitModalOpen(true);
                              }}
                            >
                              Добавить привычку
                            </button>
                          </div>

                          {habitsOverview.length === 0 ? (
                            <div className="workout-empty" style={{ marginTop: 12 }}>
                              Привычек пока нет. Нажмите «Добавить привычку».
                            </div>
                          ) : (
                            <div className="habits-panel" style={{ marginTop: 12 }}>
                              {habitsCategoryFilterOptions.length > 1 ? (
                                <FilterSelect
                                  className="filter-field--category"
                                  label="Категория"
                                  value={habitsCategoryTabKey}
                                  onChange={setHabitsCategoryTabKey}
                                  options={habitsCategoryFilterOptions}
                                />
                              ) : activeHabitsCategoryGroup?.showHeader ? (
                                <div className="category-group__header">
                                  <h4 className="category-group__title">
                                    {activeHabitsCategoryGroup.categoryName}
                                  </h4>
                                  <span
                                    className="category-group__count"
                                    title="Выполнено сегодня из всего в категории"
                                  >
                                    {activeHabitsCategoryGroup.doneCount} /{' '}
                                    {activeHabitsCategoryGroup.totalCount}
                                  </span>
                                </div>
                              ) : null}

                              <h4 style={{ margin: '12px 0 8px', fontSize: 14 }}>
                                Период
                              </h4>
                              <DatePeriodFilter
                                preset={habitDatePeriodPreset}
                                onPresetChange={setHabitDatePeriodPreset}
                                from={habitFilterDateFrom}
                                to={habitFilterDateTo}
                                onFromChange={setHabitFilterDateFrom}
                                onToChange={setHabitFilterDateTo}
                                options={HABIT_DATE_PERIOD_OPTIONS}
                                onApplyPreset={(v) =>
                                  applyHabitFilterPreset(Number(v))
                                }
                              />
                              <HabitPeriodAnalyticsPanel summary={habitPeriodAnalytics} />
                              <div
                                className="habit-analytics-legend"
                                aria-hidden="true"
                              >
                                <span className="habit-analytics-legend__item">
                                  <span className="habit-status-cell habit-status-cell--none" />
                                  без отметки
                                </span>
                                <span className="habit-analytics-legend__item">
                                  <span className="habit-status-cell habit-status-cell--partial" />
                                  частично
                                </span>
                                <span className="habit-analytics-legend__item">
                                  <span className="habit-status-cell habit-status-cell--done" />
                                  выполнено
                                </span>
                                <span className="habit-analytics-legend__item">
                                  <span className="habit-status-cell habit-status-cell--failed" />
                                  провалено
                                </span>
                              </div>

                              {activeHabitsCategoryGroup?.items?.length > 0 ? (
                              <div
                                className="users-table-wrap users-table-wrap--habits"
                                role="tabpanel"
                                aria-label={
                                  activeHabitsCategoryGroup.tabKey === '__all__'
                                    ? 'Активные привычки: все категории'
                                    : activeHabitsCategoryGroup.tabLabel
                                      ? `Активные привычки: ${activeHabitsCategoryGroup.tabLabel}`
                                      : 'Активные привычки без категории'
                                }
                              >
                                <table className="users-table habit-analytics-table">
                                  <thead>
                                    <tr>
                                      <th>Привычка</th>
                                      {activeHabitsCategoryGroup.showCategoryColumn ? (
                                        <th>Категория</th>
                                      ) : null}
                                      <th>Серия</th>
                                      <th>Освоение</th>
                                      <th>Вчера</th>
                                      <th>Сегодня</th>
                                      <th>Период</th>
                                      <th aria-label="Действия" />
                                    </tr>
                                  </thead>
                                  <tbody>
                                    {activeHabitsCategoryGroup.items.map((h) => {
                                      const statusByDate =
                                        habitsCheckinsByHabitId[String(h.id)] ??
                                        {};
                                      const today = getTodayIsoDate();
                                      const yesterday = getIsoDateDaysAgo(1);
                                      const created = habitCreatedIsoDate(h);
                                      const canMarkYesterday =
                                        !created || created <= yesterday;
                                      const yesterdayStatus = canMarkYesterday
                                        ? resolveHabitStatusForDate(
                                            statusByDate,
                                            h,
                                            yesterday,
                                            null,
                                          )
                                        : null;
                                      const todayStatus = resolveHabitTodayStatus(
                                        statusByDate,
                                        h,
                                        today,
                                      );
                                      const displayDates = getHabitDisplayDates(h);
                                      return (
                                        <tr key={h.id}>
                                          <td
                                            className="habit-table__name"
                                            data-label="Привычка"
                                          >
                                            {h.name}
                                          </td>
                                          {activeHabitsCategoryGroup.showCategoryColumn ? (
                                            <td
                                              className="habit-table__category"
                                              data-label="Категория"
                                            >
                                              {h.categoryName || '—'}
                                            </td>
                                          ) : null}
                                          <td
                                            className="habit-table__streak"
                                            data-label="Серия"
                                          >
                                            {h.currentStreakDays ?? 0} дн.
                                          </td>
                                          <td
                                            className={`habit-table__mastery${h.isMastered ? ' habit-table__mastery--done' : ''}`}
                                            data-label="Освоение"
                                            title={
                                              h.isMastered
                                                ? 'Привычка освоена'
                                                : Number(h.daysToMaster) > 0
                                                  ? `Нужна серия ${h.daysToMaster} дн. подряд (done)`
                                                  : 'Авто-освоение отключено'
                                            }
                                          >
                                            {habitMasteryLabel(h)}
                                          </td>
                                          <td
                                            className="habit-table__mark"
                                            data-label="Вчера"
                                          >
                                            {canMarkYesterday ? (
                                              <button
                                                type="button"
                                                className="habit-today-btn"
                                                title={`Вчера: ${habitStatusLabel(yesterdayStatus)}. Нажмите для смены`}
                                                aria-label={`${h.name}, вчера: ${habitStatusLabel(yesterdayStatus)}`}
                                                onClick={() =>
                                                  cycleHabitCheckinStatus(
                                                    h,
                                                    yesterday,
                                                  )
                                                }
                                              >
                                                <span
                                                  className={habitStatusCellClass(
                                                    yesterdayStatus,
                                                  )}
                                                />
                                              </button>
                                            ) : (
                                              <span
                                                className="habit-status-cell habit-status-cell--none habit-status-cell--readonly"
                                                title="Привычка создана сегодня"
                                                aria-hidden="true"
                                              />
                                            )}
                                          </td>
                                          <td
                                            className="habit-table__mark"
                                            data-label="Сегодня"
                                          >
                                            <button
                                              type="button"
                                              className="habit-today-btn"
                                              title={`Сегодня: ${habitStatusLabel(todayStatus)}. Нажмите для смены`}
                                              aria-label={`${h.name}, сегодня: ${habitStatusLabel(todayStatus)}`}
                                              onClick={() =>
                                                cycleHabitCheckinStatus(h, today)
                                              }
                                            >
                                              <span
                                                className={habitStatusCellClass(
                                                  todayStatus,
                                                )}
                                              />
                                            </button>
                                          </td>
                                          <td
                                            className="habit-table__period"
                                            data-label="Период"
                                          >
                                            {displayDates.length === 0 ? (
                                              <span className="subtitle">
                                                Укажите период
                                              </span>
                                            ) : (
                                              <div
                                                className="habit-analytics-track"
                                                role="img"
                                                aria-label={`${h.name}: отметки за период`}
                                              >
                                                {displayDates.map((d) => {
                                                  const status =
                                                    statusByDate[d] ?? null;
                                                  return (
                                                    <span
                                                      key={`${h.id}-${d}`}
                                                      className={habitStatusCellClass(
                                                        status,
                                                      )}
                                                      title={`${d}: ${habitStatusLabel(status)}`}
                                                    />
                                                  );
                                                },
                                                )}
                                              </div>
                                            )}
                                          </td>
                                          <td
                                            className="habit-table__actions"
                                            data-label="Действия"
                                          >
                                            <div className="habit-row-actions">
                                              <button
                                                type="button"
                                                className="icon-action-btn"
                                                title="Редактировать"
                                                aria-label={`Редактировать: ${h.name}`}
                                                onClick={() => openEditHabitModal(h)}
                                              >
                                                ✏️
                                              </button>
                                              <button
                                                type="button"
                                                className="icon-action-btn icon-action-btn--delete"
                                                title="Удалить привычку"
                                                aria-label={`Удалить: ${h.name}`}
                                                onClick={() =>
                                                  setPendingDeleteHabit({
                                                    id: h.id,
                                                    name: h.name,
                                                  })
                                                }
                                              >
                                                ×
                                              </button>
                                            </div>
                                          </td>
                                        </tr>
                                      );
                                    })}
                                  </tbody>
                                </table>
                              </div>
                              ) : (
                                <p className="subtitle" style={{ margin: '12px 0 0' }}>
                                  {habitsMasteredOverview.length > 0
                                    ? 'Все привычки в этой категории освоены — отметки ниже.'
                                    : 'Нет активных привычек в выбранной категории.'}
                                </p>
                              )}

                              {masteredHabitsInView.length > 0 ? (
                                <section
                                  className="habits-mastered-section"
                                  aria-label="Освоенные привычки"
                                >
                                  <h4 className="habits-mastered-section__title">
                                    Освоенные привычки
                                    <span className="habits-mastered-section__count">
                                      {masteredHabitsInView.length}
                                    </span>
                                  </h4>
                                  <div className="users-table-wrap users-table-wrap--habits users-table-wrap--habits-mastered">
                                    <table className="users-table habit-analytics-table habit-analytics-table--mastered">
                                      <thead>
                                        <tr>
                                          <th>Привычка</th>
                                          {(activeHabitsCategoryGroup?.showCategoryColumn ??
                                            masteredHabitsShowCategoryColumn) ? (
                                            <th>Категория</th>
                                          ) : null}
                                          <th>Серия</th>
                                          <th>Освоение</th>
                                          <th>Вчера</th>
                                          <th>Сегодня</th>
                                          <th>Период</th>
                                        </tr>
                                      </thead>
                                      <tbody>
                                        {masteredHabitsInView.map((h) => {
                                          const statusByDate =
                                            habitsCheckinsByHabitId[String(h.id)] ??
                                            {};
                                          const today = getTodayIsoDate();
                                          const yesterday = getIsoDateDaysAgo(1);
                                          const created = habitCreatedIsoDate(h);
                                          const canMarkYesterday =
                                            !created || created <= yesterday;
                                          const yesterdayStatus = canMarkYesterday
                                            ? resolveHabitStatusForDate(
                                                statusByDate,
                                                h,
                                                yesterday,
                                                null,
                                              )
                                            : null;
                                          const todayStatus = resolveHabitTodayStatus(
                                            statusByDate,
                                            h,
                                            today,
                                          );
                                          const displayDates = getHabitDisplayDates(h);
                                          const showCategoryColumn =
                                            activeHabitsCategoryGroup?.showCategoryColumn ??
                                            masteredHabitsShowCategoryColumn;
                                          return (
                                            <tr key={h.id}>
                                              <td
                                                className="habit-table__name"
                                                data-label="Привычка"
                                              >
                                                {h.name}
                                              </td>
                                              {showCategoryColumn ? (
                                                <td
                                                  className="habit-table__category"
                                                  data-label="Категория"
                                                >
                                                  {h.categoryName || '—'}
                                                </td>
                                              ) : null}
                                              <td
                                                className="habit-table__streak"
                                                data-label="Серия"
                                              >
                                                {h.currentStreakDays ?? 0} дн.
                                              </td>
                                              <td
                                                className="habit-table__mastery habit-table__mastery--done"
                                                data-label="Освоение"
                                                title="Привычка освоена"
                                              >
                                                {habitMasteryLabel(h)}
                                              </td>
                                              <td
                                                className="habit-table__mark"
                                                data-label="Вчера"
                                              >
                                                {canMarkYesterday ? (
                                                  <button
                                                    type="button"
                                                    className="habit-today-btn"
                                                    title={`Вчера: ${habitStatusLabel(yesterdayStatus)}. Нажмите для смены`}
                                                    aria-label={`${h.name}, вчера: ${habitStatusLabel(yesterdayStatus)}`}
                                                    onClick={() =>
                                                      cycleHabitCheckinStatus(
                                                        h,
                                                        yesterday,
                                                      )
                                                    }
                                                  >
                                                    <span
                                                      className={habitStatusCellClass(
                                                        yesterdayStatus,
                                                      )}
                                                    />
                                                  </button>
                                                ) : (
                                                  <span
                                                    className="habit-status-cell habit-status-cell--none habit-status-cell--readonly"
                                                    title="Привычка создана сегодня"
                                                    aria-hidden="true"
                                                  />
                                                )}
                                              </td>
                                              <td
                                                className="habit-table__mark"
                                                data-label="Сегодня"
                                              >
                                                <button
                                                  type="button"
                                                  className="habit-today-btn"
                                                  title={`Сегодня: ${habitStatusLabel(todayStatus)}. Нажмите для смены`}
                                                  aria-label={`${h.name}, сегодня: ${habitStatusLabel(todayStatus)}`}
                                                  onClick={() =>
                                                    cycleHabitCheckinStatus(h, today)
                                                  }
                                                >
                                                  <span
                                                    className={habitStatusCellClass(
                                                      todayStatus,
                                                    )}
                                                  />
                                                </button>
                                              </td>
                                              <td
                                                className="habit-table__period"
                                                data-label="Период"
                                              >
                                                {displayDates.length === 0 ? (
                                                  <span className="subtitle">
                                                    Укажите период
                                                  </span>
                                                ) : (
                                                  <div
                                                    className="habit-analytics-track"
                                                    role="img"
                                                    aria-label={`${h.name}: отметки за период`}
                                                  >
                                                    {displayDates.map((d) => {
                                                      const status =
                                                        statusByDate[d] ?? null;
                                                      return (
                                                        <span
                                                          key={`${h.id}-${d}`}
                                                          className={habitStatusCellClass(
                                                            status,
                                                          )}
                                                          title={`${d}: ${habitStatusLabel(status)}`}
                                                        />
                                                      );
                                                    })}
                                                  </div>
                                                )}
                                              </td>
                                            </tr>
                                          );
                                        })}
                                      </tbody>
                                    </table>
                                  </div>
                                </section>
                              ) : null}
                            </div>
                          )}
                        </>
                    </section>
                  </section>
                )}

                {tab === 'todos' && hasPermission(APP_PERMISSION.Todos) && (
                  <section className="card-grid">
                    <section className="card full-span">
                      <h3>Задачи</h3>
                      <div className="todos-panel">
                          <div className="row todos-panel__toolbar">
                            <button
                              type="button"
                              onClick={() => {
                                setTodoTitleDraft('');
                                setTodoDueDateDraft('');
                                setTodoCategoryDraft('');
                                setIsCreateTodoModalOpen(true);
                              }}
                            >
                              Добавить задачу
                            </button>
                          </div>

                          <h4 className="todos-panel__heading">Период задач</h4>
                          <DatePeriodFilter
                            className="todos-panel__date-row row--inline"
                            preset={todoDatePeriodPreset}
                            onPresetChange={setTodoDatePeriodPreset}
                            from={todoFilterDateFrom}
                            to={todoFilterDateTo}
                            onFromChange={setTodoFilterDateFrom}
                            onToChange={setTodoFilterDateTo}
                            options={TODO_DATE_PERIOD_OPTIONS}
                            onApplyPreset={applyTodoFilterPreset}
                          >
                            <FilterSelect
                              label="Статус"
                              value={todoStatusFilter}
                              onChange={setTodoStatusFilter}
                              options={todoStatusFilterOptions}
                            />
                          </DatePeriodFilter>

                          <TodoPeriodAnalyticsPanel summary={todoPeriodAnalytics} />

                          {todos.length === 0 ? (
                            <div className="workout-empty" style={{ marginTop: 8 }}>
                              Задач пока нет. Нажмите «Добавить задачу».
                            </div>
                          ) : todosFilteredByStatus.length === 0 ? (
                            <div className="workout-empty" style={{ marginTop: 8 }}>
                              {todoStatusFilter === 'open'
                                ? 'Нет открытых задач за выбранный период.'
                                : 'Нет выполненных задач за выбранный период.'}
                            </div>
                          ) : activeTodosCategoryGroup ? (
                            <div className="todos-panel__list">
                              {todosCategoryFilterOptions.length > 1 ? (
                                <FilterSelect
                                  className="filter-field--category todos-panel__category-filter"
                                  label="Категория"
                                  value={todosCategoryTabKey}
                                  onChange={setTodosCategoryTabKey}
                                  options={todosCategoryFilterOptions}
                                />
                              ) : null}
                              {activeTodosCategoryGroup.showHeader ? (
                                <div className="category-group__header">
                                  <h4 className="category-group__title">
                                    {activeTodosCategoryGroup.categoryName}
                                  </h4>
                                  <span
                                    className="category-group__count"
                                    title="Готово из всего в категории"
                                  >
                                    {activeTodosCategoryGroup.doneCount} /{' '}
                                    {activeTodosCategoryGroup.totalCount}
                                  </span>
                                </div>
                              ) : null}
                              <div
                                className="users-table-wrap users-table-wrap--todos"
                                role="tabpanel"
                                aria-label={
                                  activeTodosCategoryGroup.tabKey === '__all__'
                                    ? 'Задачи: все категории'
                                    : activeTodosCategoryGroup.tabLabel
                                      ? `Задачи: ${activeTodosCategoryGroup.tabLabel}`
                                      : 'Задачи без категории'
                                }
                              >
                                <table className="users-table todos-table">
                                  <thead>
                                    <tr>
                                      <th aria-sort={todoTableSortAria('title')}>
                                        <button
                                          type="button"
                                          className="table-sort-btn"
                                          onClick={() =>
                                            cycleTodoTableSort('title')
                                          }
                                        >
                                          Задача
                                          <span
                                            className="table-sort-btn__icon"
                                            aria-hidden="true"
                                          >
                                            {todoTableSort.key === 'title'
                                              ? todoTableSort.dir === 'asc'
                                                ? '↑'
                                                : '↓'
                                              : '↕'}
                                          </span>
                                        </button>
                                      </th>
                                      {activeTodosCategoryGroup.showCategoryColumn ? (
                                        <th
                                          aria-sort={todoTableSortAria(
                                            'category',
                                          )}
                                        >
                                          <button
                                            type="button"
                                            className="table-sort-btn"
                                            onClick={() =>
                                              cycleTodoTableSort('category')
                                            }
                                          >
                                            Категория
                                            <span
                                              className="table-sort-btn__icon"
                                              aria-hidden="true"
                                            >
                                              {todoTableSort.key === 'category'
                                                ? todoTableSort.dir === 'asc'
                                                  ? '↑'
                                                  : '↓'
                                                : '↕'}
                                            </span>
                                          </button>
                                        </th>
                                      ) : null}
                                      <th
                                        aria-sort={todoTableSortAria('dueDate')}
                                      >
                                        <button
                                          type="button"
                                          className="table-sort-btn"
                                          onClick={() =>
                                            cycleTodoTableSort('dueDate')
                                          }
                                        >
                                          Дедлайн
                                          <span
                                            className="table-sort-btn__icon"
                                            aria-hidden="true"
                                          >
                                            {todoTableSort.key === 'dueDate'
                                              ? todoTableSort.dir === 'asc'
                                                ? '↑'
                                                : '↓'
                                              : '↕'}
                                          </span>
                                        </button>
                                      </th>
                                      <th
                                        aria-sort={todoTableSortAria('status')}
                                      >
                                        <button
                                          type="button"
                                          className="table-sort-btn"
                                          onClick={() =>
                                            cycleTodoTableSort('status')
                                          }
                                        >
                                          Статус
                                          <span
                                            className="table-sort-btn__icon"
                                            aria-hidden="true"
                                          >
                                            {todoTableSort.key === 'status'
                                              ? todoTableSort.dir === 'asc'
                                                ? '↑'
                                                : '↓'
                                              : '↕'}
                                          </span>
                                        </button>
                                      </th>
                                      <th aria-label="Действия" />
                                    </tr>
                                  </thead>
                                  <tbody>
                                    {sortedActiveTodoItems.map((t) => {
                                      const isDone = Boolean(t.doneDate);
                                      const overdue = isTodoOverdue(
                                        t,
                                        getTodayIsoDate,
                                      );
                                      return (
                                        <tr
                                          key={t.id}
                                          className={
                                            isDone
                                              ? 'todos-table__row--done'
                                              : overdue
                                                ? 'todos-table__row--overdue'
                                                : undefined
                                          }
                                        >
                                          <td
                                            className={`todos-table__title${isDone ? ' todos-table__title--done' : ''}`}
                                            data-label="Задача"
                                          >
                                            {t.title}
                                            {overdue ? (
                                              <span className="todo-overdue-badge">
                                                Просрочено
                                              </span>
                                            ) : null}
                                          </td>
                                          {activeTodosCategoryGroup.showCategoryColumn ? (
                                            <td
                                              className="todos-table__category"
                                              data-label="Категория"
                                            >
                                              {t.categoryName || '—'}
                                            </td>
                                          ) : null}
                                          <td
                                            className={`todos-table__due${overdue ? ' todos-table__due--overdue' : ''}`}
                                            data-label="Дедлайн"
                                          >
                                            {t.dueDate || '—'}
                                          </td>
                                          <td
                                            className={`todos-table__status${isDone ? ' todos-table__status--done' : overdue ? ' todos-table__status--overdue' : ''}`}
                                            data-label="Статус"
                                          >
                                            {isDone
                                              ? `Готово: ${t.doneDate}`
                                              : overdue
                                                ? 'Просрочено'
                                                : 'Открыто'}
                                          </td>
                                          <td className="todos-table__actions">
                                            {!isDone ? (
                                              <div className="todo-row-actions">
                                                <button
                                                  type="button"
                                                  className="icon-action-btn icon-action-btn--done"
                                                  title="Отметить выполненным"
                                                  aria-label="Отметить выполненным"
                                                  onClick={() =>
                                                    setPendingCompleteTodo({
                                                      id: t.id,
                                                      title: t.title,
                                                    })
                                                  }
                                                >
                                                  ✓
                                                </button>
                                                <button
                                                  type="button"
                                                  className="icon-action-btn icon-action-btn--delete"
                                                  title="Удалить"
                                                  aria-label="Удалить задачу"
                                                  onClick={() =>
                                                    setPendingDeleteTodo({
                                                      id: t.id,
                                                      title: t.title,
                                                    })
                                                  }
                                                >
                                                  ×
                                                </button>
                                              </div>
                                            ) : null}
                                          </td>
                                        </tr>
                                      );
                                    })}
                                  </tbody>
                                </table>
                              </div>
                            </div>
                          ) : null}
                        </div>
                    </section>
                  </section>
                )}

                {tab === 'investments' &&
                  hasPermission(APP_PERMISSION.Investments) && (
                  <section className="card-grid">
                    <section className="card full-span">
                      <h3>Инвестиции</h3>
                      <p className="subtitle">
                        Портфель и учёт позиций. Раздел в разработке — данные
                        пока загружаются с сервера-заглушки.
                      </p>
                      {investmentSummary?.isStub ? (
                        <p className="subtitle" style={{ marginTop: 4 }}>
                          Сводка и список позиций пока пустые.
                        </p>
                      ) : null}
                      <div className="tracking-analytics-stats">
                        <div className="tracking-analytics-stat">
                          <span className="tracking-analytics-stat__label">
                            Вложено
                          </span>
                          <span className="tracking-analytics-stat__value">
                            {formatMoney(
                              investmentSummary?.totalInvested ?? 0,
                              investmentSummary?.currency,
                            )}
                          </span>
                        </div>
                        <div className="tracking-analytics-stat">
                          <span className="tracking-analytics-stat__label">
                            Текущая стоимость
                          </span>
                          <span className="tracking-analytics-stat__value">
                            {formatMoney(
                              investmentSummary?.totalCurrentValue ?? 0,
                              investmentSummary?.currency,
                            )}
                          </span>
                        </div>
                        <div className="tracking-analytics-stat">
                          <span className="tracking-analytics-stat__label">
                            P/L
                          </span>
                          <span className="tracking-analytics-stat__value">
                            {formatMoney(
                              investmentSummary?.totalProfitLoss ?? 0,
                              investmentSummary?.currency,
                            )}
                          </span>
                          <span className="tracking-analytics-stat__hint">
                            {Number.isFinite(
                              Number(investmentSummary?.totalProfitLossPercent),
                            )
                              ? `${Number(investmentSummary.totalProfitLossPercent).toFixed(2)}%`
                              : '0%'}
                          </span>
                        </div>
                        <div className="tracking-analytics-stat">
                          <span className="tracking-analytics-stat__label">
                            Позиций
                          </span>
                          <span className="tracking-analytics-stat__value">
                            {investmentSummary?.positionsCount ?? 0}
                          </span>
                        </div>
                      </div>
                      <div className="row">
                        <button
                          type="button"
                          onClick={() => {
                            resetInvestmentDraft();
                            setInvestmentsMessage('');
                            setIsCreateInvestmentModalOpen(true);
                          }}
                        >
                          Добавить позицию
                        </button>
                        <button
                          type="button"
                          className="ghost-btn"
                          onClick={() => loadInvestments()}
                        >
                          Обновить
                        </button>
                      </div>
                      {investmentsMessage ? (
                        <p
                          className="subtitle"
                          style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}
                        >
                          {investmentsMessage}
                        </p>
                      ) : null}
                      <div className="users-table-wrap" style={{ marginTop: 12 }}>
                        <table className="users-table">
                          <thead>
                            <tr>
                              <th>Тикер</th>
                              <th>Название</th>
                              <th>Тип</th>
                              <th>Кол-во</th>
                              <th>Ср. цена</th>
                              <th>Текущая</th>
                              <th>Валюта</th>
                              <th>Заметки</th>
                            </tr>
                          </thead>
                          <tbody>
                            {investments.length === 0 && (
                              <tr>
                                <td colSpan="8">
                                  Позиций пока нет. Нажмите «Добавить позицию».
                                </td>
                              </tr>
                            )}
                            {investments.map((row) => (
                              <tr key={row.id}>
                                <td>{row.ticker || '—'}</td>
                                <td>{row.name || '—'}</td>
                                <td>
                                  {formatInvestmentAssetType(row.assetType)}
                                </td>
                                <td>{row.quantity ?? '—'}</td>
                                <td>
                                  {formatMoney(row.averagePrice, row.currency)}
                                </td>
                                <td>
                                  {row.currentPrice != null
                                    ? formatMoney(row.currentPrice, row.currency)
                                    : '—'}
                                </td>
                                <td>{row.currency || 'RUB'}</td>
                                <td>{row.notes || '—'}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </section>
                  </section>
                )}

                {tab === 'admin' && isAdmin && (
                  <section className="card-grid">
                    <section className="card full-span">
                      <h3>Админ</h3>
                      <p className="subtitle">
                        Управление пользователями, категориями, ИИ-помощниками
                        и диалогами.
                      </p>
                      <SegmentTabs
                        variant="primary"
                        className="segment-tabs--narrow"
                        ariaLabel="Разделы админки"
                      >
                        {visibleAdminSubTabs.map((subTabId) => (
                          <SegmentTab
                            key={subTabId}
                            active={adminSubTab === subTabId}
                            onClick={() => setAdminSubTab(subTabId)}
                          >
                            {adminSubTabLabels[subTabId] || subTabId}
                          </SegmentTab>
                        ))}
                      </SegmentTabs>
                    </section>

                    {adminSubTab === 'users-manage' &&
                      hasPermission(APP_PERMISSION.AdminUsers) && (
                    <section className="card full-span">
                      <h3>Управление пользователем</h3>
                      <div className="row">
                        <button onClick={() => setIsCreateUserModalOpen(true)}>
                          Новый пользователь
                        </button>
                      </div>
                      <div className="users-table-wrap">
                        <table className="users-table">
                          <thead>
                            <tr>
                              <th>Логин</th>
                              <th>Роли</th>
                              <th>Действия</th>
                            </tr>
                          </thead>
                          <tbody>
                            {users.length === 0 && (
                              <tr>
                                <td colSpan="3">Пользователи не загружены</td>
                              </tr>
                            )}
                            {users.map((u) => (
                              <tr key={`manage-${u.id}`}>
                                <td>{u.username}</td>
                                <td>{formatUserRolesLabel(normalizeUserRoles(u))}</td>
                                <td className="admin-actions">
                                  <button
                                    onClick={() => openEditModal(u)}
                                    title="Редактировать"
                                  >
                                    ✏️
                                  </button>
                                  <button onClick={() => openPasswordModal(u)}>
                                    Пароль
                                  </button>
                                  <button
                                    className="danger-btn danger-btn--icon"
                                    onClick={() => openDeleteModal(u)}
                                    title="Удалить"
                                  >
                                    ×
                                  </button>
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </section>
                    )}

                    {adminSubTab === 'roles' &&
                      hasPermission(APP_PERMISSION.AdminRoles) && (
                    <section className="card full-span">
                      <h3>Роли пользователей</h3>
                      <p className="subtitle">
                        Код роли используется в JWT. Для каждой роли настрой,
                        какие разделы приложения и админки ей доступны. Изменения
                        прав вступают в силу после повторного входа пользователя.
                      </p>
                      <div className="row">
                        <button type="button" onClick={openAdminRoleCreateModal}>
                          Новая роль
                        </button>
                      </div>
                      <div className="users-table-wrap">
                        <table className="users-table">
                          <thead>
                            <tr>
                              <th>Код</th>
                              <th>Название</th>
                              <th>Описание</th>
                              <th>Порядок</th>
                              <th>Активна</th>
                              <th>Тип</th>
                              <th>Действия</th>
                            </tr>
                          </thead>
                          <tbody>
                            {adminRoles.length === 0 && (
                              <tr>
                                <td colSpan="7">Роли не загружены.</td>
                              </tr>
                            )}
                            {adminRoles.map((row) => (
                              <tr key={row.id}>
                                <td>{row.name}</td>
                                <td>{row.label || '—'}</td>
                                <td>{row.description || '—'}</td>
                                <td>{row.sortOrder}</td>
                                <td>{row.isActive ? 'Да' : 'Нет'}</td>
                                <td>{row.isSystem ? 'Системная' : 'Пользовательская'}</td>
                                <td className="admin-actions">
                                  <button
                                    type="button"
                                    onClick={() => openRolePermissionsModal(row)}
                                    title="Права доступа"
                                  >
                                    Права
                                  </button>
                                  <button
                                    type="button"
                                    onClick={() => openAdminRoleEditModal(row)}
                                    title="Редактировать"
                                  >
                                    ✏️
                                  </button>
                                  {!row.isSystem ? (
                                    <button
                                      type="button"
                                      className="danger-btn danger-btn--icon"
                                      onClick={() => setPendingDeleteRole(row)}
                                      title="Удалить"
                                    >
                                      ×
                                    </button>
                                  ) : null}
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </section>
                    )}

                    {adminSubTab === 'categories' &&
                      hasPermission(APP_PERMISSION.AdminCategories) && (
                    <section className="card full-span">
                      <h3>Категории привычек и задач</h3>
                      <div className="row">
                        <button
                          type="button"
                          onClick={openAdminCategoryCreateModal}
                        >
                          Новая категория
                        </button>
                      </div>
                      <div className="users-table-wrap">
                        <table className="users-table">
                          <thead>
                            <tr>
                              <th>Название</th>
                              <th>Описание</th>
                              <th>Порядок</th>
                              <th>Активна</th>
                              <th>Действия</th>
                            </tr>
                          </thead>
                          <tbody>
                            {adminCategories.length === 0 && (
                              <tr>
                                <td colSpan="5">Категорий пока нет.</td>
                              </tr>
                            )}
                            {adminCategories.map((row) => (
                              <tr key={row.id}>
                                <td>{row.name}</td>
                                <td>{row.description || '—'}</td>
                                <td>{row.sortOrder}</td>
                                <td>{row.isActive ? 'Да' : 'Нет'}</td>
                                <td className="admin-actions">
                                  <button
                                    type="button"
                                    onClick={() => openAdminCategoryEditModal(row)}
                                    title="Редактировать"
                                  >
                                    ✏️
                                  </button>
                                  <button
                                    type="button"
                                    className="danger-btn danger-btn--icon"
                                    onClick={() => setPendingDeleteCategory(row)}
                                    title="Удалить"
                                  >
                                    ×
                                  </button>
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </section>
                    )}

                    {adminSubTab === 'users-profiles' &&
                      hasPermission(APP_PERMISSION.AdminProfiles) && (
                    <section className="card full-span">
                      <h3>Пользователи</h3>
                      <div className="users-table-wrap">
                        <table className="users-table">
                          <thead>
                            <tr>
                              <th>Логин</th>
                              <th>Роли</th>
                              <th>Имя</th>
                              <th>Фамилия</th>
                              <th>Дата рождения</th>
                              <th>Рост (см)</th>
                              <th>Вес (кг)</th>
                              <th>Телефон</th>
                              <th>Город</th>
                              <th>О себе</th>
                              <th>AI-саммари</th>
                              <th>Создан</th>
                              <th>ID пользователя</th>
                            </tr>
                          </thead>
                          <tbody>
                            {users.length === 0 && (
                              <tr>
                                <td colSpan="13">Пользователи не загружены</td>
                              </tr>
                            )}
                            {users.map((u) => (
                              <tr key={u.id}>
                                <td>{u.username}</td>
                                <td>{formatUserRolesLabel(normalizeUserRoles(u))}</td>
                                <td>{u.firstName || '-'}</td>
                                <td>{u.lastName || '-'}</td>
                                <td>{u.birthDate || '-'}</td>
                                <td>{u.heightCm ?? '-'}</td>
                                <td>{u.weightKg ?? '-'}</td>
                                <td>{u.phone || '-'}</td>
                                <td>{u.city || '-'}</td>
                                <td>{u.about || '-'}</td>
                                <td className="admin-ai-summary-cell">
                                  {u.aiSummary
                                    ? `${String(u.aiSummary).slice(0, 120)}${String(u.aiSummary).length > 120 ? '…' : ''}`
                                    : '-'}
                                </td>
                                <td>
                                  {u.createdAtUtc
                                    ? new Date(u.createdAtUtc).toLocaleString()
                                    : '-'}
                                </td>
                                <td>{u.id}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                      <p className="subtitle">
                        Режим только чтение: редактирование данных пользователя
                        для администратора отключено.
                      </p>
                    </section>
                    )}

                    {adminSubTab === 'ai-assistants' &&
                      hasPermission(APP_PERMISSION.AdminAiAssistants) && (
                    <section className="card full-span">
                      <h3>ИИ помощники</h3>
                      <div className="row">
                        <button
                          type="button"
                          onClick={openAssistantCreateModal}
                        >
                          Новый помощник
                        </button>
                      </div>
                      <div className="users-table-wrap">
                        <table className="users-table">
                          <thead>
                            <tr>
                              <th>Название</th>
                              <th>Код</th>
                              <th>Порядок</th>
                              <th>Активен</th>
                              <th>Промпт</th>
                              <th>Действия</th>
                            </tr>
                          </thead>
                          <tbody>
                            {adminAiAssistants.length === 0 && (
                              <tr>
                                <td colSpan="6">Нет записей</td>
                              </tr>
                            )}
                            {adminAiAssistants.map((row) => (
                              <tr key={row.id}>
                                <td>{row.name}</td>
                                <td
                                  style={{
                                    fontFamily: 'ui-monospace, monospace',
                                    fontSize: '0.9em',
                                  }}
                                >
                                  {row.assistantCode || '—'}
                                </td>
                                <td>{row.sortOrder}</td>
                                <td>{row.isActive ? 'Да' : 'Нет'}</td>
                                <td className="admin-ai-summary-cell">
                                  {row.systemPrompt
                                    ? `${String(row.systemPrompt).slice(0, 80)}${String(row.systemPrompt).length > 80 ? '…' : ''}`
                                    : '—'}
                                </td>
                                <td className="admin-actions">
                                  <button
                                    type="button"
                                    onClick={() => openAssistantEditModal(row)}
                                    title="Редактировать"
                                  >
                                    ✏️
                                  </button>
                                  <button
                                    type="button"
                                    className={
                                      String(chatAssistantPreviewId) ===
                                      String(row.id)
                                        ? ''
                                        : 'ghost-btn'
                                    }
                                    disabled={!row.isActive}
                                    title={
                                      row.isActive
                                        ? 'Открыть пробный чат с этим помощником (без смены выбора на вкладке ИИ)'
                                        : 'Сначала отметьте помощника как активный'
                                    }
                                    onClick={() => {
                                      if (!row.isActive) return;
                                      setChatAssistantPreviewId(row.id);
                                      setAdminSubTab('ai-test-chat');
                                    }}
                                  >
                                    Тест чата
                                  </button>
                                  {!row.isSystem && (
                                    <button
                                      type="button"
                                      className="danger-btn danger-btn--icon"
                                      onClick={() =>
                                        setPendingDeleteAssistantId(row.id)
                                      }
                                      title="Удалить"
                                    >
                                      ×
                                    </button>
                                  )}
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </section>
                    )}

                    {adminSubTab === 'ai-test-chat' &&
                      hasPermission(APP_PERMISSION.AdminAiTestChat) && (
                    <section
                      className="card full-span"
                      ref={adminAssistantTestChatPanelRef}
                    >
                      <h3>Пробный чат с помощником</h3>
                      <p className="subtitle">
                        Проверка ответов под промптом помощника для твоего
                        аккаунта. Не меняет пользовательский выбор «Включить» на
                        вкладке ИИ. Для запроса API нужен{' '}
                        <strong>активный</strong> помощник — кнопка «Тест чата»
                        на вкладке «ИИ помощники» неактивна, пока в карточке
                        помощника не стоит «Активен».
                      </p>
                      <p className="subtitle">
                        {chatAssistantPreviewId && !previewAssistantRow ? (
                          <>
                            Ссылка на помощника устарела.
                            <button
                              type="button"
                              className="ghost-btn"
                              style={{ marginLeft: 8 }}
                              onClick={() => setChatAssistantPreviewId(null)}
                            >
                              Сбросить
                            </button>
                          </>
                        ) : previewAssistantRow ? (
                          <>
                            <strong>Пробный режим:</strong> сообщения уходят с
                            помощником «
                            <strong>{previewAssistantRow.name}</strong>
                            ». Доп. поля подставляются как на вкладке ИИ для
                            этого помощника.
                            <button
                              type="button"
                              className="ghost-btn"
                              style={{ marginLeft: 8 }}
                              onClick={() => setChatAssistantPreviewId(null)}
                            >
                              Выйти из пробного режима
                            </button>
                          </>
                        ) : (
                          <>
                            Выбери помощника кнопкой «Тест чата» на вкладке
                            «ИИ помощники» или пиши на вкладке «ИИ» с
                            включённым помощником — там же заполняются доп.
                            поля.
                          </>
                        )}
                      </p>
                      <p className="subtitle">
                        Тот же список диалогов, что и на вкладке ИИ; можно
                        создать отдельный диалог только для проверок.
                      </p>
                      <div className="row">
                        <select
                          value={currentDialogId}
                          onChange={(e) => {
                            setCurrentDialogId(e.target.value);
                            loadDialogMessages(e.target.value);
                          }}
                        >
                          <option value="">Нет диалогов</option>
                          {dialogOptions}
                        </select>
                        <button
                          type="button"
                          onClick={() => {
                            setAiDialogTitleDraft('');
                            setAiDialogModalKind('new');
                          }}
                        >
                          + Новый
                        </button>
                        <button
                          type="button"
                          onClick={() => {
                            if (!currentDialogId) return;
                            const current = dialogs.find(
                              (d) => d.id === currentDialogId,
                            );
                            setAiDialogTitleDraft(
                              current?.title || 'Новый диалог',
                            );
                            setAiDialogModalKind('rename');
                          }}
                          title="Переименовать"
                        >
                          ✏️
                        </button>
                        <button
                          type="button"
                          className="danger-btn danger-btn--icon"
                          onClick={() => {
                            if (!currentDialogId) return;
                            setAiDialogModalKind('delete');
                          }}
                          title="Удалить"
                        >
                          ×
                        </button>
                      </div>
                      <div className="chat-messages">
                        {chatMessages.map((m, i) => (
                          <div
                            key={i}
                            className={`chat-msg ${m.role === 'user' ? 'user' : 'assistant'}`}
                          >
                            {m.content}
                          </div>
                        ))}
                      </div>
                      <div className="row">
                        <input
                          value={chatPrompt}
                          onChange={(e) => setChatPrompt(e.target.value)}
                          placeholder="Сообщение для проверки…"
                        />
                        <button type="button" onClick={sendChat}>
                          Отправить
                        </button>
                      </div>
                    </section>
                    )}

                    {adminSubTab === 'dialogs' &&
                      hasPermission(APP_PERMISSION.AdminDialogs) && (
                    <section className="card full-span">
                      <h3>Диалоги (Админ)</h3>
                      <label>Пользователь</label>
                      <div className="row">
                        <select
                          value={adminDialogUserId}
                          onChange={(e) => {
                            const nextUserId = e.target.value;
                            setAdminDialogUserId(nextUserId);
                            setAdminCurrentDialogId('');
                            setAdminDialogMessages([]);
                            loadAdminDialogs(nextUserId);
                          }}
                        >
                          <option value="">Все пользователи</option>
                          {adminUserOptions}
                        </select>
                      </div>

                      <label>Диалог</label>
                      <div className="row">
                        <select
                          value={adminCurrentDialogId}
                          onChange={(e) => {
                            setAdminCurrentDialogId(e.target.value);
                            loadAdminDialogMessages(e.target.value);
                          }}
                        >
                          <option value="">Нет диалогов</option>
                          {adminDialogOptions}
                        </select>
                        <button
                          onClick={() => {
                            setAdminDialogTitleDraft('');
                            setAdminDialogModalKind('create');
                          }}
                        >
                          Новый диалог
                        </button>
                        <button
                          onClick={() => {
                            if (!adminCurrentDialogId) return;
                            const current = adminDialogs.find(
                              (d) => d.id === adminCurrentDialogId,
                            );
                            setAdminDialogTitleDraft(
                              current?.title || 'Новый диалог',
                            );
                            setAdminDialogModalKind('rename');
                          }}
                          title="Переименовать"
                        >
                          ✏️
                        </button>
                        <button
                          className="danger-btn danger-btn--icon"
                          onClick={() => {
                            if (!adminCurrentDialogId) return;
                            setAdminDialogModalKind('delete');
                          }}
                          title="Удалить"
                        >
                          ×
                        </button>
                      </div>

                      {adminCurrentDialogId ? (
                        <p
                          style={{
                            color: 'var(--muted)',
                            margin: '0.35rem 0 0.5rem',
                            fontSize: '0.92rem',
                          }}
                        >
                          Помощник: {adminSelectedDialogAssistantLabel}
                        </p>
                      ) : null}

                      <div className="chat-messages small">
                        {adminDialogMessages.length === 0 && (
                          <div className="chat-msg assistant">
                            Нет сообщений для выбранного диалога.
                          </div>
                        )}
                        {adminDialogMessages.map((m) => (
                          <div
                            key={m.id}
                            className={`chat-msg ${m.role === 'user' ? 'user' : 'assistant'}`}
                          >
                            {m.content}
                          </div>
                        ))}
                      </div>
                    </section>
                    )}
                  </section>
                )}

                {tab === 'admin' && isAdmin && isCreateUserModalOpen && (
                  <ModalShell
                    open={isCreateUserModalOpen}
                    onClose={() => setIsCreateUserModalOpen(false)}
                  >
                    <h3>Создать пользователя</h3>
                    <label>Логин</label>
                    <input
                      value={adminCreateUsername}
                      onChange={(e) => setAdminCreateUsername(e.target.value)}
                      placeholder="логин"
                    />
                    <label>Пароль</label>
                    <input
                      value={adminCreatePassword}
                      onChange={(e) => setAdminCreatePassword(e.target.value)}
                      placeholder="пароль"
                      type="password"
                    />
                    <span className="filter-field__label">Роли</span>
                    <div className="role-checkbox-group">
                      {userRoleOptions.map((opt) => (
                        <label key={opt.value} className="role-checkbox">
                          <input
                            type="checkbox"
                            checked={adminCreateRoles.includes(opt.value)}
                            onChange={(e) =>
                              setAdminCreateRoles((prev) =>
                                toggleUserRoleSelection(
                                  prev,
                                  opt.value,
                                  e.target.checked,
                                ),
                              )
                            }
                          />
                          {opt.label}
                        </label>
                      ))}
                    </div>
                    <div className="row">
                      <button onClick={createAdminUser}>Создать</button>
                      <button
                        className="ghost-btn"
                        onClick={() => setIsCreateUserModalOpen(false)}
                      >
                        Отмена
                      </button>
                    </div>
                  </ModalShell>
                )}
                {tab === 'admin' &&
                  isAdmin &&
                  isEditUserModalOpen &&
                  selectedAdminUser && (
                    <ModalShell
                      open={isEditUserModalOpen}
                      onClose={() => setIsEditUserModalOpen(false)}
                    >
                      <h3>Редактировать пользователя</h3>
                      <label>Логин</label>
                      <input
                        value={editUserName}
                        onChange={(e) => setEditUserName(e.target.value)}
                      />
                      <span className="filter-field__label">Роли</span>
                      <div className="role-checkbox-group">
                        {userRoleOptions.map((opt) => (
                          <label key={opt.value} className="role-checkbox">
                            <input
                              type="checkbox"
                              checked={editUserRoles.includes(opt.value)}
                              onChange={(e) =>
                                setEditUserRoles((prev) =>
                                  toggleUserRoleSelection(
                                    prev,
                                    opt.value,
                                    e.target.checked,
                                  ),
                                )
                              }
                            />
                            {opt.label}
                          </label>
                        ))}
                      </div>
                      <div className="row">
                        <button onClick={saveAdminUserFromModal}>
                          Сохранить
                        </button>
                        <button
                          className="ghost-btn"
                          onClick={() => setIsEditUserModalOpen(false)}
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}
                {tab === 'admin' &&
                  isAdmin &&
                  isPasswordModalOpen &&
                  selectedAdminUser && (
                    <ModalShell
                      open={isPasswordModalOpen}
                      onClose={() => setIsPasswordModalOpen(false)}
                    >
                      <h3>Сменить пароль</h3>
                      <p className="subtitle">{selectedAdminUser.username}</p>
                      <label>Новый пароль</label>
                      <input
                        value={newPasswordValue}
                        onChange={(e) => setNewPasswordValue(e.target.value)}
                        type="password"
                        placeholder="новый пароль"
                      />
                      <div className="row">
                        <button onClick={saveAdminPasswordFromModal}>
                          Сохранить пароль
                        </button>
                        <button
                          className="ghost-btn"
                          onClick={() => setIsPasswordModalOpen(false)}
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}
                {tab === 'admin' &&
                  isAdmin &&
                  isDeleteModalOpen &&
                  selectedAdminUser && (
                    <ModalShell
                      open={isDeleteModalOpen}
                      onClose={() => setIsDeleteModalOpen(false)}
                    >
                      <h3>Удалить пользователя</h3>
                      <p className="subtitle">
                        Ты точно хочешь удалить:{' '}
                        <b>{selectedAdminUser.username}</b>?
                      </p>
                      <div className="row">
                        <button
                          className="danger-btn danger-btn--icon"
                          onClick={deleteAdminUserFromModal}
                          title="Удалить"
                        >
                          ×
                        </button>
                        <button
                          className="ghost-btn"
                          onClick={() => setIsDeleteModalOpen(false)}
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}
                {tab === 'admin' &&
                  isAdmin &&
                  (adminCategoryModalKind === 'create' ||
                    adminCategoryModalKind === 'edit') && (
                    <ModalShell
                      open={Boolean(adminCategoryModalKind)}
                      onClose={() => setAdminCategoryModalKind(null)}
                    >
                      <h3>
                        {adminCategoryModalKind === 'create'
                          ? 'Новая категория'
                          : 'Редактировать категорию'}
                      </h3>
                      <label>Название</label>
                      <input
                        value={adminCategoryDraft.name}
                        onChange={(e) =>
                          setAdminCategoryDraft((d) => ({
                            ...d,
                            name: e.target.value,
                          }))
                        }
                        placeholder="Например: Здоровье"
                      />
                      <label>Описание</label>
                      <input
                        value={adminCategoryDraft.description}
                        onChange={(e) =>
                          setAdminCategoryDraft((d) => ({
                            ...d,
                            description: e.target.value,
                          }))
                        }
                        placeholder="Необязательно"
                      />
                      <label>Порядок</label>
                      <input
                        type="number"
                        value={adminCategoryDraft.sortOrder}
                        onChange={(e) =>
                          setAdminCategoryDraft((d) => ({
                            ...d,
                            sortOrder: Number(e.target.value),
                          }))
                        }
                      />
                      <label
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: 8,
                        }}
                      >
                        <input
                          type="checkbox"
                          checked={adminCategoryDraft.isActive}
                          onChange={(e) =>
                            setAdminCategoryDraft((d) => ({
                              ...d,
                              isActive: e.target.checked,
                            }))
                          }
                        />
                        Активна
                      </label>
                      <div className="row">
                        <button onClick={submitAdminCategoryModal}>
                          Сохранить
                        </button>
                        <button
                          className="ghost-btn"
                          onClick={() => setAdminCategoryModalKind(null)}
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}
                {tab === 'admin' && isAdmin && pendingDeleteCategory && (
                  <ModalShell
                    open={Boolean(pendingDeleteCategory)}
                    onClose={() => setPendingDeleteCategory(null)}
                  >
                    <h3>Удалить категорию</h3>
                    <p className="subtitle">
                      Удалить категорию «{pendingDeleteCategory.name || '—'}»?
                    </p>
                    <div className="row">
                      <button
                        className="danger-btn danger-btn--icon"
                        onClick={confirmDeleteCategory}
                        title="Удалить"
                      >
                        ×
                      </button>
                      <button
                        className="ghost-btn"
                        onClick={() => setPendingDeleteCategory(null)}
                      >
                        Отмена
                      </button>
                    </div>
                    </ModalShell>
                  )}
                {tab === 'admin' &&
                  isAdmin &&
                  (adminRoleModalKind === 'create' ||
                    adminRoleModalKind === 'edit') && (
                    <ModalShell
                      open={Boolean(adminRoleModalKind)}
                      onClose={() => setAdminRoleModalKind(null)}
                    >
                      <h3>
                        {adminRoleModalKind === 'create'
                          ? 'Новая роль'
                          : 'Редактировать роль'}
                      </h3>
                      {adminRoleModalKind === 'create' ? (
                        <>
                          <label>Код (латиница)</label>
                          <input
                            value={adminRoleDraft.name}
                            onChange={(e) =>
                              setAdminRoleDraft((d) => ({
                                ...d,
                                name: e.target.value,
                              }))
                            }
                            placeholder="Например: Moderator"
                          />
                        </>
                      ) : (
                        <p className="subtitle">
                          Код: <b>{adminRoleDraft.name}</b>
                          {adminRoleDraft.isSystem ? ' (системная)' : ''}
                        </p>
                      )}
                      <label>Название</label>
                      <input
                        value={adminRoleDraft.label}
                        onChange={(e) =>
                          setAdminRoleDraft((d) => ({
                            ...d,
                            label: e.target.value,
                          }))
                        }
                        placeholder="Отображаемое название"
                      />
                      <label>Описание</label>
                      <input
                        value={adminRoleDraft.description}
                        onChange={(e) =>
                          setAdminRoleDraft((d) => ({
                            ...d,
                            description: e.target.value,
                          }))
                        }
                        placeholder="Необязательно"
                      />
                      <label>Порядок сортировки</label>
                      <input
                        type="number"
                        value={adminRoleDraft.sortOrder}
                        onChange={(e) =>
                          setAdminRoleDraft((d) => ({
                            ...d,
                            sortOrder: Number(e.target.value) || 0,
                          }))
                        }
                      />
                      <label className="role-checkbox">
                        <input
                          type="checkbox"
                          checked={adminRoleDraft.isActive}
                          disabled={adminRoleDraft.isSystem}
                          onChange={(e) =>
                            setAdminRoleDraft((d) => ({
                              ...d,
                              isActive: e.target.checked,
                            }))
                          }
                        />
                        Активна
                      </label>
                      <div className="row">
                        <button type="button" onClick={submitAdminRoleModal}>
                          Сохранить
                        </button>
                        <button
                          type="button"
                          className="ghost-btn"
                          onClick={() => setAdminRoleModalKind(null)}
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}
                {tab === 'admin' && isAdmin && rolePermissionsModalRole && (
                  <ModalShell
                    open={Boolean(rolePermissionsModalRole)}
                    onClose={() => {
                      setRolePermissionsModalRole(null);
                      setRolePermissionDraft([]);
                    }}
                    wide
                    scroll
                  >
                    <h3>
                      Права роли «
                      {rolePermissionsModalRole.label ||
                        rolePermissionsModalRole.name}
                      »
                    </h3>
                    <p className="subtitle">
                      Отметь функции, доступные пользователям с этой ролью.
                    </p>
                    {Object.entries(permissionsByCategory).map(
                      ([category, items]) => (
                        <section key={category} style={{ marginBottom: 16 }}>
                          <span className="filter-field__label">
                            {PERMISSION_CATEGORY_LABELS[category] || category}
                          </span>
                          <div className="role-checkbox-group">
                            {items.map((item) => (
                              <label
                                key={item.code}
                                className="role-checkbox"
                                title={item.description || item.code}
                              >
                                <input
                                  type="checkbox"
                                  checked={rolePermissionDraft.includes(
                                    item.code,
                                  )}
                                  onChange={(e) =>
                                    toggleRolePermissionDraft(
                                      item.code,
                                      e.target.checked,
                                    )
                                  }
                                />
                                {item.label || item.code}
                              </label>
                            ))}
                          </div>
                        </section>
                      ),
                    )}
                    <div className="row">
                      <button type="button" onClick={submitRolePermissionsModal}>
                        Сохранить
                      </button>
                      <button
                        type="button"
                        className="ghost-btn"
                        onClick={() => {
                          setRolePermissionsModalRole(null);
                          setRolePermissionDraft([]);
                        }}
                      >
                        Отмена
                      </button>
                    </div>
                  </ModalShell>
                )}
                {tab === 'admin' && isAdmin && pendingDeleteRole && (
                  <ModalShell
                    open={Boolean(pendingDeleteRole)}
                    onClose={() => setPendingDeleteRole(null)}
                  >
                    <h3>Удалить роль</h3>
                    <p className="subtitle">
                      Удалить роль «{pendingDeleteRole.label || pendingDeleteRole.name}»?
                    </p>
                    <div className="row">
                      <button
                        className="danger-btn danger-btn--icon"
                        onClick={confirmDeleteRole}
                        title="Удалить"
                      >
                        ×
                      </button>
                      <button
                        className="ghost-btn"
                        onClick={() => setPendingDeleteRole(null)}
                      >
                        Отмена
                      </button>
                    </div>
                  </ModalShell>
                )}
                {tab === 'admin' &&
                  isAdmin &&
                  (assistantModalKind === 'create' ||
                    assistantModalKind === 'edit') && (
                    <ModalShell
                      open={Boolean(assistantModalKind)}
                      onClose={() => {
                        setAssistantModalKind(null);
                        setAdminExtraFieldsList([]);
                      }}
                      wide
                      scroll
                    >
                      <h3>
                        {assistantModalKind === 'create'
                          ? 'Новый ИИ помощник'
                          : 'ИИ помощник'}
                      </h3>
                      <p className="subtitle">
                        Системный промпт, настройки и дополнительные поля для
                        пользователей — в одном окне.
                      </p>
                      <label>Название</label>
                      <input
                        value={assistantDraft.name}
                        onChange={(e) =>
                          setAssistantDraft((d) => ({
                            ...d,
                            name: e.target.value,
                          }))
                        }
                        placeholder="Краткое имя"
                      />
                      <label>Описание (для панели пользователя)</label>
                      <input
                        value={assistantDraft.description}
                        onChange={(e) =>
                          setAssistantDraft((d) => ({
                            ...d,
                            description: e.target.value,
                          }))
                        }
                        placeholder="Необязательно"
                      />
                      <label>Системный промпт</label>
                      <p className="subtitle" style={{ margin: '0 0 8px' }}>
                        В тексте можно использовать подстановки из полей
                        помощника:{' '}
                        <code style={{ fontFamily: 'ui-monospace, monospace' }}>
                          {'{{ключ}}'}
                        </code>{' '}
                        (латиница, как в колонке «Ключ»).
                      </p>
                      <textarea
                        value={assistantDraft.systemPrompt}
                        onChange={(e) =>
                          setAssistantDraft((d) => ({
                            ...d,
                            systemPrompt: e.target.value,
                          }))
                        }
                        rows={8}
                        placeholder="Инструкции для модели. Пример: Вес: {{weight}} кг; цель: {{goal}}"
                      />
                      <label>Настройки (JSON, необязательно)</label>
                      <textarea
                        value={assistantDraft.settingsJson}
                        onChange={(e) =>
                          setAssistantDraft((d) => ({
                            ...d,
                            settingsJson: e.target.value,
                          }))
                        }
                        rows={3}
                        placeholder='Например: {"temperature": 0.7}'
                      />
                      <label>Порядок сортировки</label>
                      <input
                        type="number"
                        value={assistantDraft.sortOrder}
                        onChange={(e) =>
                          setAssistantDraft((d) => ({
                            ...d,
                            sortOrder: Number(e.target.value),
                          }))
                        }
                      />
                      <label
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: 8,
                        }}
                      >
                        <input
                          type="checkbox"
                          checked={assistantDraft.isActive}
                          onChange={(e) =>
                            setAssistantDraft((d) => ({
                              ...d,
                              isActive: e.target.checked,
                            }))
                          }
                        />
                        Активен (доступен пользователям)
                      </label>

                      <div
                        style={{
                          marginTop: 20,
                          paddingTop: 16,
                          borderTop:
                            '1px solid var(--border-subtle, rgba(255,255,255,0.12))',
                        }}
                      >
                        <h4 style={{ marginTop: 0 }}>Дополнительные поля</h4>
                        <p className="subtitle">
                          Ключ — латиница, цифры и подчёркивание. Тип: текст,
                          многострочный текст или число. Поля заполняют
                          пользователи во вкладке ИИ.
                        </p>
                        {!assistantDraft.id ? (
                          <p className="subtitle">
                            Нажми «Сохранить помощника» ниже — после создания
                            записи здесь можно добавить поля.
                          </p>
                        ) : (
                          <>
                            <div className="row">
                              <button
                                type="button"
                                onClick={openAdminExtraFieldCreate}
                              >
                                Новое поле
                              </button>
                              <button
                                type="button"
                                className="ghost-btn"
                                onClick={() =>
                                  loadAdminExtraFields(assistantDraft.id)
                                }
                              >
                                Обновить список полей
                              </button>
                            </div>
                            <div className="users-table-wrap">
                              <table className="users-table">
                                <thead>
                                  <tr>
                                    <th>Ключ</th>
                                    <th>Подпись</th>
                                    <th>Тип</th>
                                    <th>Порядок</th>
                                    <th>Обяз.</th>
                                    <th />
                                  </tr>
                                </thead>
                                <tbody>
                                  {adminExtraFieldsList.length === 0 && (
                                    <tr>
                                      <td colSpan="6">Нет полей</td>
                                    </tr>
                                  )}
                                  {adminExtraFieldsList.map((f) => (
                                    <tr key={f.id}>
                                      <td>{f.fieldKey}</td>
                                      <td>{f.label}</td>
                                      <td>{f.fieldType}</td>
                                      <td>{f.sortOrder}</td>
                                      <td>{f.isRequired ? 'Да' : 'Нет'}</td>
                                      <td className="admin-actions">
                                        <button
                                          type="button"
                                          onClick={() =>
                                            openAdminExtraFieldEdit(f)
                                          }
                                          title="Редактировать"
                                        >
                                          ✏️
                                        </button>
                                        {!f.isSystem && (
                                          <button
                                            type="button"
                                            className="danger-btn danger-btn--icon"
                                            onClick={() =>
                                              setPendingDeleteExtraField({
                                                assistantId: assistantDraft.id,
                                                fieldId: f.id,
                                              })
                                            }
                                            title="Удалить"
                                          >
                                            ×
                                          </button>
                                        )}
                                      </td>
                                    </tr>
                                  ))}
                                </tbody>
                              </table>
                            </div>
                          </>
                        )}
                      </div>

                      <div className="row">
                        <button type="button" onClick={submitAssistantModal}>
                          Сохранить помощника
                        </button>
                        <button
                          type="button"
                          className="ghost-btn"
                          onClick={() => {
                            setAssistantModalKind(null);
                            setAdminExtraFieldsList([]);
                          }}
                        >
                          Закрыть
                        </button>
                      </div>
                    </ModalShell>
                  )}
                {tab === 'admin' && isAdmin && pendingDeleteAssistantId && (
                  <ModalShell
                    open={Boolean(pendingDeleteAssistantId)}
                    onClose={() => setPendingDeleteAssistantId(null)}
                  >
                    <h3>Удалить помощника</h3>
                    <p className="subtitle">
                      Удалить эту запись? У пользователей с выбранным помощником
                      выбор будет сброшен.
                    </p>
                    <div className="row">
                      <button
                        type="button"
                        className="danger-btn danger-btn--icon"
                        onClick={confirmDeleteAssistant}
                        title="Удалить"
                      >
                        ×
                      </button>
                      <button
                        type="button"
                        className="ghost-btn"
                        onClick={() => setPendingDeleteAssistantId(null)}
                      >
                        Отмена
                      </button>
                    </div>
                  </ModalShell>
                )}
                {tab === 'admin' &&
                  isAdmin &&
                  (adminExtraFieldModalKind === 'create' ||
                    adminExtraFieldModalKind === 'edit') && (
                    <ModalShell
                      open={Boolean(adminExtraFieldModalKind)}
                      onClose={() => setAdminExtraFieldModalKind(null)}
                    >
                      <h3>
                        {adminExtraFieldModalKind === 'create'
                          ? 'Новое поле'
                          : 'Редактировать поле'}
                      </h3>
                      <label>Ключ поля</label>
                      <input
                        value={adminExtraFieldDraft.fieldKey}
                        onChange={(e) =>
                          setAdminExtraFieldDraft((d) => ({
                            ...d,
                            fieldKey: e.target.value,
                          }))
                        }
                        placeholder="например training_goal"
                        disabled={adminExtraFieldModalKind === 'edit'}
                      />
                      {adminExtraFieldModalKind === 'edit' && (
                        <p className="subtitle">
                          Ключ после создания не меняется.
                        </p>
                      )}
                      <label>Подпись для пользователя</label>
                      <input
                        value={adminExtraFieldDraft.label}
                        onChange={(e) =>
                          setAdminExtraFieldDraft((d) => ({
                            ...d,
                            label: e.target.value,
                          }))
                        }
                        placeholder="Отображаемое название"
                      />
                      <label>Тип</label>
                      <select
                        value={adminExtraFieldDraft.fieldType}
                        onChange={(e) =>
                          setAdminExtraFieldDraft((d) => ({
                            ...d,
                            fieldType: e.target.value,
                          }))
                        }
                      >
                        <option value="text">Текст</option>
                        <option value="textarea">Многострочный текст</option>
                        <option value="number">Число</option>
                      </select>
                      <label>Порядок</label>
                      <input
                        type="number"
                        value={adminExtraFieldDraft.sortOrder}
                        onChange={(e) =>
                          setAdminExtraFieldDraft((d) => ({
                            ...d,
                            sortOrder: Number(e.target.value),
                          }))
                        }
                      />
                      <label
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: 8,
                        }}
                      >
                        <input
                          type="checkbox"
                          checked={adminExtraFieldDraft.isRequired}
                          onChange={(e) =>
                            setAdminExtraFieldDraft((d) => ({
                              ...d,
                              isRequired: e.target.checked,
                            }))
                          }
                        />
                        Обязательное поле
                      </label>
                      <div className="row">
                        <button
                          type="button"
                          onClick={submitAdminExtraFieldModal}
                        >
                          Сохранить
                        </button>
                        <button
                          type="button"
                          className="ghost-btn"
                          onClick={() => setAdminExtraFieldModalKind(null)}
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}
                {tab === 'admin' && isAdmin && pendingDeleteExtraField && (
                  <ModalShell
                    open={Boolean(pendingDeleteExtraField)}
                    onClose={() => setPendingDeleteExtraField(null)}
                  >
                    <h3>Удалить поле</h3>
                    <p className="subtitle">
                      Значения пользователей для этого ключа перестанут
                      использоваться.
                    </p>
                    <div className="row">
                      <button
                        type="button"
                        className="danger-btn danger-btn--icon"
                        onClick={confirmDeleteExtraField}
                        title="Удалить"
                      >
                        ×
                      </button>
                      <button
                        type="button"
                        className="ghost-btn"
                        onClick={() => setPendingDeleteExtraField(null)}
                      >
                        Отмена
                      </button>
                    </div>
                  </ModalShell>
                )}

                {tab === 'workouts' &&
                  workoutsSubTab === 'strength' &&
                  strengthSubTab === 'manage' &&
                  workoutsManageSubTab === 'add' &&
                  isProgramModalOpen && (
                    <div className="modal-backdrop">
                      <div className="modal-card modal-card--wide modal-card--scroll">
                        <h3>
                          {editingProgramId
                            ? 'Редактирование программы'
                            : 'Новая программа'}
                        </h3>
                        <label>Код программы (латиницей, опционально)</label>
                        <input
                          value={programCode}
                          onChange={(e) => setProgramCode(e.target.value)}
                          placeholder="например, upper-body-a"
                        />
                        <label>Название программы</label>
                        <input
                          value={programDay}
                          onChange={(e) => setProgramDay(e.target.value)}
                          placeholder="Верх тела A"
                        />
                        <label>Заметки</label>
                        <textarea
                          value={programNotes}
                          onChange={(e) => setProgramNotes(e.target.value)}
                          rows={3}
                          placeholder="Комментарий к программе"
                        />
                        <label>Упражнение</label>
                        <div className="row">
                          <div className="exercise-picker-combobox">
                            <input
                              type="text"
                              role="combobox"
                              aria-expanded={programExercisePickerOpen}
                              aria-autocomplete="list"
                              value={programExercisePickerSearch}
                              onChange={(e) => {
                                setProgramExercisePickerSearch(e.target.value);
                                setProgramExercisePickerOpen(true);
                              }}
                              onFocus={() => setProgramExercisePickerOpen(true)}
                              onBlur={() => {
                                window.setTimeout(
                                  () => setProgramExercisePickerOpen(false),
                                  150,
                                );
                              }}
                              placeholder="Начни вводить название упражнения"
                            />
                            {programExercisePickerOpen &&
                            filteredProgramExerciseCatalog.length > 0 ? (
                              <ul
                                className="exercise-picker-combobox__list"
                                role="listbox"
                              >
                                {filteredProgramExerciseCatalog.map(
                                  (exercise) => (
                                    <li
                                      key={exercise.id}
                                      role="option"
                                      onMouseDown={(e) => e.preventDefault()}
                                      onClick={() =>
                                        selectProgramExerciseForPicker(exercise)
                                      }
                                    >
                                      {exercise.name}
                                    </li>
                                  ),
                                )}
                              </ul>
                            ) : null}
                            {programExercisePickerOpen &&
                            programExercisePickerSearch.trim() &&
                            filteredProgramExerciseCatalog.length === 0 ? (
                              <ul className="exercise-picker-combobox__list">
                                <li className="exercise-picker-combobox__empty">
                                  Ничего не найдено по запросу «
                                  {programExercisePickerSearch.trim()}»
                                </li>
                              </ul>
                            ) : null}
                          </div>
                          <button
                            type="button"
                            onClick={() =>
                              selectedCatalogExercise &&
                              addExerciseToProgram(selectedCatalogExercise)
                            }
                            disabled={!selectedCatalogExercise}
                          >
                            Добавить в программу
                          </button>
                        </div>
                        <h4>Упражнения в программе</h4>
                        <div className="users-table-wrap program-draft-table-wrap">
                          <table className="users-table program-draft-table">
                            <thead>
                              <tr>
                                <th>Упражнение</th>
                                <th>Комментарий</th>
                                <th>Действия</th>
                              </tr>
                            </thead>
                            <tbody>
                              {programExercisesDraft.length === 0 && (
                                <tr>
                                  <td
                                    colSpan="3"
                                    className="program-draft-empty"
                                  >
                                    Нет строк. Выбери упражнение выше и нажми
                                    «Добавить в программу» — появится новая
                                    строка.
                                  </td>
                                </tr>
                              )}
                              {programExercisesDraft.map((exercise) => (
                                <tr key={exercise.id}>
                                  <td className="program-draft-name">
                                    {exercise.name}
                                  </td>
                                  <td>
                                    <textarea
                                      className="program-draft-comment"
                                      value={exercise.comment || ''}
                                      onChange={(e) =>
                                        updateProgramExerciseComment(
                                          exercise.id,
                                          e.target.value,
                                        )
                                      }
                                      rows={2}
                                      placeholder="Например: техника, темп, акцент"
                                    />
                                  </td>
                                  <td>
                                    <button
                                      type="button"
                                      className="danger-btn danger-btn--icon"
                                      onClick={() =>
                                        removeProgramExercise(exercise.id)
                                      }
                                      title="Удалить"
                                    >
                                      ×
                                    </button>
                                  </td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                        <div className="row">
                          <button onClick={saveProgramToDb}>
                            Сохранить программу
                          </button>
                          <button
                            className="ghost-btn"
                            onClick={closeProgramModal}
                          >
                            Отмена
                          </button>
                        </div>
                      </div>
                    </div>
                  )}

                {tab === 'workouts' &&
                  workoutsSubTab === 'strength' &&
                  strengthSubTab === 'manage' &&
                  workoutsManageSubTab === 'add' &&
                  isProgramDeleteModalOpen &&
                  pendingDeleteProgram && (
                    <div className="modal-backdrop">
                      <div className="modal-card">
                        <h3>Удалить программу</h3>
                        <p className="subtitle">
                          Ты точно хочешь удалить программу:{' '}
                          <b>
                            {pendingDeleteProgram.day ||
                              pendingDeleteProgram.sessionCode}
                          </b>
                          ?
                        </p>
                        <div className="row">
                          <button
                            className="danger-btn danger-btn--icon"
                            onClick={deleteProgramFromModal}
                            title="Удалить"
                          >
                            ×
                          </button>
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
                {tab === 'profile' && isProfileEditModalOpen && (
                  <ModalShell
                    open={isProfileEditModalOpen}
                    onClose={() => setIsProfileEditModalOpen(false)}
                    scroll
                    titleId="profile-edit-title"
                  >
                    <h3 id="profile-edit-title">Редактировать профиль</h3>
                    <label>Имя</label>
                    <input
                      value={profileFirstName}
                      onChange={(e) => setProfileFirstName(e.target.value)}
                      placeholder="Имя"
                    />
                    <label>Фамилия</label>
                    <input
                      value={profileLastName}
                      onChange={(e) => setProfileLastName(e.target.value)}
                      placeholder="Фамилия"
                    />
                    <div className="row profile-row">
                      <div>
                        <label>Дата рождения</label>
                        <input
                          type="date"
                          value={profileBirthDate}
                          onChange={(e) => setProfileBirthDate(e.target.value)}
                        />
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
                    <input
                      value={profilePhone}
                      onChange={(e) => setProfilePhone(e.target.value)}
                      placeholder="+7..."
                    />
                    <label>Город</label>
                    <input
                      value={profileCity}
                      onChange={(e) => setProfileCity(e.target.value)}
                      placeholder="Алматы"
                    />
                    <label>О себе</label>
                    <textarea
                      value={profileAbout}
                      onChange={(e) => setProfileAbout(e.target.value)}
                      placeholder="Любая полезная информация"
                      rows={4}
                    />
                    <div className="row">
                      <button onClick={saveMyProfile}>Сохранить</button>
                      <button
                        className="ghost-btn"
                        onClick={() => setIsProfileEditModalOpen(false)}
                      >
                        Отмена
                      </button>
                    </div>
                  </ModalShell>
                )}

                {isTelegramUnlinkConfirmOpen && (
                  <ModalShell
                    open={isTelegramUnlinkConfirmOpen}
                    onClose={() => setIsTelegramUnlinkConfirmOpen(false)}
                    titleId="telegram-unlink-confirm-title"
                  >
                    <div className="confirm-modal">
                      <h3
                        id="telegram-unlink-confirm-title"
                        className="confirm-modal__title"
                      >
                        Отвязать Telegram?
                      </h3>
                      <p className="confirm-modal__hint">
                        Бот перестанет записывать вес в ваш дневник. Привязку
                        можно будет настроить снова.
                      </p>
                      <div className="confirm-modal__actions">
                        <button
                          type="button"
                          className="danger-btn"
                          onClick={() => void unlinkTelegramAccount()}
                        >
                          Отвязать
                        </button>
                        <button
                          type="button"
                          className="ghost-btn"
                          onClick={() => setIsTelegramUnlinkConfirmOpen(false)}
                        >
                          Отмена
                        </button>
                      </div>
                    </div>
                  </ModalShell>
                )}

                {tab === 'profile' && isTelegramLinkModalOpen && (
                  <ModalShell
                    open={isTelegramLinkModalOpen}
                    onClose={() => setIsTelegramLinkModalOpen(false)}
                    scroll
                    titleId="telegram-link-title"
                  >
                    <h3 id="telegram-link-title">Telegram</h3>
                    {profileTelegramLinked ? (
                      <>
                        <p className="subtitle">
                          Аккаунт привязан. Вес из бота записывается в дневник
                          (вкладка «Прогресс»).
                        </p>
                        <button
                          type="button"
                          className="danger-btn"
                          onClick={() => setIsTelegramUnlinkConfirmOpen(true)}
                        >
                          Отвязать
                        </button>
                      </>
                    ) : (
                      <>
                        <p className="subtitle">
                          Создайте ссылку, откройте её в Telegram и нажмите
                          «Start» у бота. Ссылка одноразовая и действует
                          ограниченное время.
                        </p>
                        {telegramLinkError ? (
                          <p className="error-text" role="alert">
                            {telegramLinkError}
                          </p>
                        ) : null}
                        <button
                          type="button"
                          onClick={() => void createTelegramDeepLink()}
                          disabled={telegramLinkLoading}
                        >
                          {telegramLinkLoading
                            ? 'Загрузка…'
                            : telegramLinkUrl
                              ? 'Создать новую ссылку'
                              : 'Создать ссылку'}
                        </button>
                        {telegramLinkUrl ? (
                          <>
                            {telegramLinkExpiresAt ? (
                              <p className="subtitle" style={{ marginTop: 10 }}>
                                Действует до:{' '}
                                {new Date(
                                  telegramLinkExpiresAt,
                                ).toLocaleString()}
                              </p>
                            ) : null}
                            <div className="row" style={{ marginTop: 12 }}>
                              <button
                                type="button"
                                onClick={() => {
                                  window.open(
                                    telegramLinkUrl,
                                    '_blank',
                                    'noopener,noreferrer',
                                  );
                                }}
                              >
                                Открыть Telegram
                              </button>
                              <button
                                type="button"
                                className="ghost-btn"
                                onClick={() => {
                                  void navigator.clipboard?.writeText(
                                    telegramLinkUrl,
                                  );
                                }}
                              >
                                Копировать ссылку
                              </button>
                            </div>
                          </>
                        ) : null}
                      </>
                    )}
                    <div className="row" style={{ marginTop: 16 }}>
                      <button
                        type="button"
                        className="ghost-btn"
                        onClick={() => setIsTelegramLinkModalOpen(false)}
                      >
                        Закрыть
                      </button>
                    </div>
                  </ModalShell>
                )}

                {tab === 'habits' && editingHabit && (
                  <ModalShell
                    open={Boolean(editingHabit)}
                    onClose={() => setEditingHabit(null)}
                    titleId="habit-edit-title"
                  >
                    <h3 id="habit-edit-title">Редактировать привычку</h3>
                    {editingHabit.isMastered ? (
                      <p className="subtitle" style={{ marginTop: 0 }}>
                        Привычка уже освоена — можно менять название, категорию и
                        порог для справки.
                      </p>
                    ) : null}
                    <label htmlFor="habit-edit-name">Название</label>
                    <input
                      id="habit-edit-name"
                      type="text"
                      value={habitsEditName}
                      onChange={(e) => setHabitsEditName(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') void saveHabitEdit();
                      }}
                    />
                    <label htmlFor="habit-edit-category">Категория</label>
                    <select
                      id="habit-edit-category"
                      value={habitsEditCategoryId}
                      onChange={(e) => setHabitsEditCategoryId(e.target.value)}
                    >
                      <option value="">Без категории</option>
                      {userCategories.map((c) => (
                        <option key={c.id} value={c.id}>
                          {c.name}
                        </option>
                      ))}
                    </select>
                    <label htmlFor="habit-edit-days-to-master">
                      Дней подряд до освоения
                    </label>
                    <input
                      id="habit-edit-days-to-master"
                      type="number"
                      min={0}
                      max={999}
                      value={habitsEditDaysToMaster}
                      onChange={(e) => setHabitsEditDaysToMaster(e.target.value)}
                    />
                    <div className="row" style={{ marginTop: 16 }}>
                      <button type="button" onClick={() => saveHabitEdit()}>
                        Сохранить
                      </button>
                      <button
                        type="button"
                        className="ghost-btn"
                        onClick={() => setEditingHabit(null)}
                      >
                        Отмена
                      </button>
                    </div>
                  </ModalShell>
                )}

                {tab === 'habits' && isCreateHabitModalOpen && (
                  <ModalShell
                    open={isCreateHabitModalOpen}
                    onClose={() => setIsCreateHabitModalOpen(false)}
                    titleId="habit-create-title"
                  >
                    <h3 id="habit-create-title">Новая привычка</h3>
                    <label htmlFor="habit-create-name">Название</label>
                    <input
                      id="habit-create-name"
                      type="text"
                      value={habitsNewName}
                      onChange={(e) => setHabitsNewName(e.target.value)}
                      placeholder="Например: вода, зарядка"
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') void createHabit();
                      }}
                    />
                    <label htmlFor="habit-create-category">Категория</label>
                    <select
                      id="habit-create-category"
                      value={habitsNewCategoryId}
                      onChange={(e) => setHabitsNewCategoryId(e.target.value)}
                    >
                      <option value="">Без категории</option>
                      {userCategories.map((c) => (
                        <option key={c.id} value={c.id}>
                          {c.name}
                        </option>
                      ))}
                    </select>
                    <label htmlFor="habit-create-days-to-master">
                      Дней подряд до освоения
                    </label>
                    <input
                      id="habit-create-days-to-master"
                      type="number"
                      min={0}
                      max={999}
                      value={habitsNewDaysToMaster}
                      onChange={(e) => setHabitsNewDaysToMaster(e.target.value)}
                      placeholder="21"
                    />
                    <p className="subtitle" style={{ marginTop: 4 }}>
                      Сколько дней подряд со статусом «выполнено» нужно для отметки
                      «освоена». 0 — не считать автоматически.
                    </p>
                    <div className="row" style={{ marginTop: 16 }}>
                      <button type="button" onClick={() => createHabit()}>
                        Добавить
                      </button>
                      <button
                        type="button"
                        className="ghost-btn"
                        onClick={() => setIsCreateHabitModalOpen(false)}
                      >
                        Отмена
                      </button>
                    </div>
                  </ModalShell>
                )}

                {tab === 'todos' && isCreateTodoModalOpen && (
                  <ModalShell
                    open={isCreateTodoModalOpen}
                    onClose={() => setIsCreateTodoModalOpen(false)}
                    titleId="todo-create-title"
                  >
                    <h3 id="todo-create-title">Новая задача</h3>
                    <label htmlFor="todo-create-title-input">Название</label>
                    <input
                      id="todo-create-title-input"
                      type="text"
                      value={todoTitleDraft}
                      onChange={(e) => setTodoTitleDraft(e.target.value)}
                      placeholder="Например: подготовить план"
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') void createTodo();
                      }}
                    />
                    <label htmlFor="todo-create-due-date">
                      Дедлайн (опционально)
                    </label>
                    <input
                      id="todo-create-due-date"
                      type="date"
                      value={todoDueDateDraft}
                      onChange={(e) => setTodoDueDateDraft(e.target.value)}
                    />
                    <label htmlFor="todo-create-category">Категория</label>
                    <select
                      id="todo-create-category"
                      value={todoCategoryDraft}
                      onChange={(e) => setTodoCategoryDraft(e.target.value)}
                    >
                      <option value="">Без категории</option>
                      {userCategories.map((c) => (
                        <option key={c.id} value={c.id}>
                          {c.name}
                        </option>
                      ))}
                    </select>
                    <div className="row" style={{ marginTop: 16 }}>
                      <button type="button" onClick={() => createTodo()}>
                        Добавить
                      </button>
                      <button
                        type="button"
                        className="ghost-btn"
                        onClick={() => setIsCreateTodoModalOpen(false)}
                      >
                        Отмена
                      </button>
                    </div>
                  </ModalShell>
                )}

                {tab === 'investments' && isCreateInvestmentModalOpen && (
                  <ModalShell
                    open={isCreateInvestmentModalOpen}
                    onClose={() => setIsCreateInvestmentModalOpen(false)}
                    titleId="investment-create-title"
                  >
                    <h3 id="investment-create-title">Новая позиция</h3>
                    <label htmlFor="investment-create-ticker">Тикер</label>
                    <input
                      id="investment-create-ticker"
                      type="text"
                      value={investmentDraft.ticker}
                      onChange={(e) =>
                        setInvestmentDraft((prev) => ({
                          ...prev,
                          ticker: e.target.value,
                        }))
                      }
                      placeholder="SBER, FXUS, BTC"
                    />
                    <label htmlFor="investment-create-name">Название</label>
                    <input
                      id="investment-create-name"
                      type="text"
                      value={investmentDraft.name}
                      onChange={(e) =>
                        setInvestmentDraft((prev) => ({
                          ...prev,
                          name: e.target.value,
                        }))
                      }
                      placeholder="Сбербанк, ETF и т.д."
                    />
                    <label htmlFor="investment-create-type">Тип актива</label>
                    <select
                      id="investment-create-type"
                      value={investmentDraft.assetType}
                      onChange={(e) =>
                        setInvestmentDraft((prev) => ({
                          ...prev,
                          assetType: e.target.value,
                        }))
                      }
                    >
                      <option value="stock">Акции</option>
                      <option value="bond">Облигации</option>
                      <option value="etf">ETF</option>
                      <option value="crypto">Крипто</option>
                      <option value="other">Другое</option>
                    </select>
                    <div className="row">
                      <div>
                        <label htmlFor="investment-create-quantity">
                          Количество
                        </label>
                        <input
                          id="investment-create-quantity"
                          type="number"
                          min="0"
                          step="any"
                          value={investmentDraft.quantity}
                          onChange={(e) =>
                            setInvestmentDraft((prev) => ({
                              ...prev,
                              quantity: e.target.value,
                            }))
                          }
                        />
                      </div>
                      <div>
                        <label htmlFor="investment-create-avg-price">
                          Средняя цена
                        </label>
                        <input
                          id="investment-create-avg-price"
                          type="number"
                          min="0"
                          step="any"
                          value={investmentDraft.averagePrice}
                          onChange={(e) =>
                            setInvestmentDraft((prev) => ({
                              ...prev,
                              averagePrice: e.target.value,
                            }))
                          }
                        />
                      </div>
                    </div>
                    <label htmlFor="investment-create-currency">Валюта</label>
                    <select
                      id="investment-create-currency"
                      value={investmentDraft.currency}
                      onChange={(e) =>
                        setInvestmentDraft((prev) => ({
                          ...prev,
                          currency: e.target.value,
                        }))
                      }
                    >
                      <option value="RUB">RUB</option>
                      <option value="USD">USD</option>
                      <option value="EUR">EUR</option>
                    </select>
                    <label htmlFor="investment-create-notes">
                      Заметки (опционально)
                    </label>
                    <input
                      id="investment-create-notes"
                      type="text"
                      value={investmentDraft.notes}
                      onChange={(e) =>
                        setInvestmentDraft((prev) => ({
                          ...prev,
                          notes: e.target.value,
                        }))
                      }
                    />
                    {investmentsMessage ? (
                      <p
                        className="subtitle"
                        style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}
                      >
                        {investmentsMessage}
                      </p>
                    ) : null}
                    <div className="row" style={{ marginTop: 16 }}>
                      <button
                        type="button"
                        onClick={() => submitCreateInvestment()}
                      >
                        Сохранить
                      </button>
                      <button
                        type="button"
                        className="ghost-btn"
                        onClick={() => setIsCreateInvestmentModalOpen(false)}
                      >
                        Отмена
                      </button>
                    </div>
                  </ModalShell>
                )}

                {tab === 'todos' && pendingCompleteTodo && (
                  <ModalShell
                    open={Boolean(pendingCompleteTodo)}
                    onClose={() => setPendingCompleteTodo(null)}
                    titleId="todo-complete-confirm-title"
                  >
                    <div className="confirm-modal">
                      <h3
                        id="todo-complete-confirm-title"
                        className="confirm-modal__title"
                      >
                        Действительно готово?
                      </h3>
                      <p className="confirm-modal__detail">
                        <span className="confirm-modal__label">Задача</span>
                        <span className="confirm-modal__value">
                          {pendingCompleteTodo.title || '—'}
                        </span>
                      </p>
                      <div className="confirm-modal__actions">
                        <button
                          type="button"
                          onClick={() => confirmCompleteTodo()}
                        >
                          Да
                        </button>
                        <button
                          type="button"
                          className="ghost-btn"
                          onClick={() => setPendingCompleteTodo(null)}
                        >
                          Нет
                        </button>
                      </div>
                    </div>
                  </ModalShell>
                )}

                {tab === 'todos' && pendingDeleteTodo && (
                  <ModalShell
                    open={Boolean(pendingDeleteTodo)}
                    onClose={() => setPendingDeleteTodo(null)}
                    titleId="todo-delete-confirm-title"
                  >
                    <div className="confirm-modal">
                      <h3
                        id="todo-delete-confirm-title"
                        className="confirm-modal__title"
                      >
                        Удалить задачу?
                      </h3>
                      <p className="confirm-modal__detail">
                        <span className="confirm-modal__label">Название</span>
                        <span className="confirm-modal__value">
                          {pendingDeleteTodo.title || '—'}
                        </span>
                      </p>
                      <div className="confirm-modal__actions">
                        <button
                          type="button"
                          className="danger-btn"
                          onClick={() => confirmDeleteTodo()}
                        >
                          Удалить
                        </button>
                        <button
                          type="button"
                          className="ghost-btn"
                          onClick={() => setPendingDeleteTodo(null)}
                        >
                          Отмена
                        </button>
                      </div>
                    </div>
                  </ModalShell>
                )}

                {tab === 'habits' && pendingDeleteHabit && (
                  <ModalShell
                    open={Boolean(pendingDeleteHabit)}
                    onClose={() => setPendingDeleteHabit(null)}
                    titleId="habit-delete-confirm-title"
                  >
                    <div className="confirm-modal">
                      <h3
                        id="habit-delete-confirm-title"
                        className="confirm-modal__title"
                      >
                        Удалить привычку?
                      </h3>
                      <p className="confirm-modal__detail">
                        <span className="confirm-modal__label">Название</span>
                        <span className="confirm-modal__value">
                          {pendingDeleteHabit.name || '—'}
                        </span>
                      </p>
                      <p className="confirm-modal__hint">
                        История отметок по этой привычке будет удалена.
                      </p>
                      <div className="confirm-modal__actions">
                        <button
                          type="button"
                          className="danger-btn"
                          onClick={() => confirmDeleteHabit()}
                        >
                          Удалить
                        </button>
                        <button
                          type="button"
                          className="ghost-btn"
                          onClick={() => setPendingDeleteHabit(null)}
                        >
                          Отмена
                        </button>
                      </div>
                    </div>
                  </ModalShell>
                )}

                {pendingDeleteWeightEntry && (
                  <ModalShell
                    open={Boolean(pendingDeleteWeightEntry)}
                    onClose={() => setPendingDeleteWeightEntry(null)}
                    titleId="weight-delete-confirm-title"
                  >
                    <div className="confirm-modal">
                      <div className="confirm-modal__badge" aria-hidden="true">
                        ⚖️
                      </div>
                      <h3
                        id="weight-delete-confirm-title"
                        className="confirm-modal__title"
                      >
                        Удалить запись веса?
                      </h3>
                      <p className="confirm-modal__detail">
                        <span className="confirm-modal__label">Дата</span>
                        <span className="confirm-modal__value">
                          {pendingDeleteWeightEntry.date || '—'}
                        </span>
                      </p>
                      <p className="confirm-modal__detail">
                        <span className="confirm-modal__label">Вес</span>
                        <span className="confirm-modal__value">
                          {pendingDeleteWeightEntry.weightKg ?? '—'} кг
                        </span>
                      </p>
                      <p className="confirm-modal__hint">
                        Запись исчезнет из таблицы и графика. Профиль и
                        AI-тренер обновятся по последнему оставшемуся весу.
                      </p>
                      <div className="confirm-modal__actions">
                        <button
                          type="button"
                          className="danger-btn"
                          onClick={confirmDeleteWeightTrackerEntry}
                        >
                          Удалить
                        </button>
                        <button
                          type="button"
                          className="ghost-btn"
                          onClick={() => setPendingDeleteWeightEntry(null)}
                        >
                          Отмена
                        </button>
                      </div>
                    </div>
                  </ModalShell>
                )}

                {(tab === 'ai' ||
                  tab === 'admin' ||
                  (tab === 'workouts' && workoutsSubTab === 'ai-trainer')) &&
                  hasAiAccess &&
                  aiDialogModalKind === 'new' && (
                    <ModalShell
                      open={aiDialogModalKind === 'new'}
                      onClose={() => {
                        setAiDialogModalKind(null);
                        setAiDialogTitleDraft('');
                      }}
                    >
                      <h3>Новый диалог</h3>
                      <label>Название</label>
                      <input
                        value={aiDialogTitleDraft}
                        onChange={(e) => setAiDialogTitleDraft(e.target.value)}
                        placeholder="Новый диалог"
                      />
                      <div className="row">
                        <button onClick={submitAiNewDialog}>Создать</button>
                        <button
                          className="ghost-btn"
                          onClick={() => {
                            setAiDialogModalKind(null);
                            setAiDialogTitleDraft('');
                          }}
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}
                {(tab === 'ai' ||
                  tab === 'admin' ||
                  (tab === 'workouts' && workoutsSubTab === 'ai-trainer')) &&
                  hasAiAccess &&
                  aiDialogModalKind === 'rename' && (
                    <ModalShell
                      open={aiDialogModalKind === 'rename'}
                      onClose={() => {
                        setAiDialogModalKind(null);
                        setAiDialogTitleDraft('');
                      }}
                    >
                      <h3>Переименовать диалог</h3>
                      <label>Название</label>
                      <input
                        value={aiDialogTitleDraft}
                        onChange={(e) => setAiDialogTitleDraft(e.target.value)}
                        placeholder="Новый диалог"
                      />
                      <div className="row">
                        <button onClick={submitAiRenameDialog}>
                          Сохранить
                        </button>
                        <button
                          className="ghost-btn"
                          onClick={() => {
                            setAiDialogModalKind(null);
                            setAiDialogTitleDraft('');
                          }}
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}
                {(tab === 'ai' ||
                  tab === 'admin' ||
                  (tab === 'workouts' && workoutsSubTab === 'ai-trainer')) &&
                  hasAiAccess &&
                  aiDialogModalKind === 'delete' && (
                    <ModalShell
                      open={aiDialogModalKind === 'delete'}
                      onClose={() => setAiDialogModalKind(null)}
                    >
                      <h3>Удалить диалог</h3>
                      <p className="subtitle">
                        Удалить текущий выбранный диалог без восстановления?
                      </p>
                      <div className="row">
                        <button
                          className="danger-btn danger-btn--icon"
                          onClick={submitAiDeleteDialog}
                          title="Удалить"
                        >
                          ×
                        </button>
                        <button
                          className="ghost-btn"
                          onClick={() => setAiDialogModalKind(null)}
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}
                {hasAiAccess && isAiExtraInfoModalOpen && (
                  <ModalShell
                    open={isAiExtraInfoModalOpen}
                    onClose={() => setIsAiExtraInfoModalOpen(false)}
                    wide
                    scroll
                  >
                    <h3>Дополнительная информация</h3>
                    <p className="subtitle">
                      Помощник:{' '}
                      <strong>
                        {aiAssistants.find(
                          (x) =>
                            String(x.id) === String(aiExtraInfoAssistantId),
                        )?.name || '—'}
                      </strong>
                      . Поля задаёт администратор для каждого помощника
                      отдельно; при чате с выбранным помощником они попадают в
                      контекст модели.
                    </p>
                    <p className="subtitle">
                      Поля веса, роста (см) и возраста синхронизированы с
                      карточкой профиля: возраст считается из даты рождения;
                      значение из профиля имеет приоритет, если оно заполнено.
                      Сохранение здесь обновляет и профиль на сервере.
                    </p>
                    {aiExtraInfoDefinitions.length === 0 ? (
                      <p className="subtitle">
                        Для этого помощника пока нет дополнительных полей.
                      </p>
                    ) : (
                      aiExtraInfoDefinitions.map((def) => (
                        <div key={def.id || def.fieldKey}>
                          <label>
                            {def.label}
                            {def.isRequired ? ' *' : ''}
                            <span
                              className="subtitle"
                              style={{ marginLeft: 6 }}
                            >
                              ({def.fieldKey})
                            </span>
                          </label>
                          {def.fieldType === 'textarea' ? (
                            <textarea
                              rows={4}
                              value={aiExtraInfoValues[def.fieldKey] ?? ''}
                              onChange={(e) =>
                                handleAiExtraFieldChange(
                                  def.fieldKey,
                                  e.target.value,
                                )
                              }
                            />
                          ) : def.fieldType === 'number' ? (
                            <input
                              type="number"
                              value={aiExtraInfoValues[def.fieldKey] ?? ''}
                              onChange={(e) =>
                                handleAiExtraFieldChange(
                                  def.fieldKey,
                                  e.target.value,
                                )
                              }
                            />
                          ) : (
                            <input
                              type="text"
                              value={aiExtraInfoValues[def.fieldKey] ?? ''}
                              onChange={(e) =>
                                handleAiExtraFieldChange(
                                  def.fieldKey,
                                  e.target.value,
                                )
                              }
                            />
                          )}
                        </div>
                      ))
                    )}
                    <div className="row">
                      <button
                        type="button"
                        onClick={submitAiExtraInfoModal}
                        disabled={aiExtraInfoDefinitions.length === 0}
                      >
                        Сохранить
                      </button>
                      <button
                        type="button"
                        className="ghost-btn"
                        onClick={() => setIsAiExtraInfoModalOpen(false)}
                      >
                        Отмена
                      </button>
                    </div>
                  </ModalShell>
                )}

                {tab === 'workouts' &&
                  workoutsSubTab === 'strength' &&
                  strengthSubTab === 'my-workout' &&
                  isActiveWorkoutModalOpen &&
                  currentWorkout && (
                    <ModalShell
                      open={isActiveWorkoutModalOpen}
                      onClose={hideActiveWorkoutModal}
                      wide
                      scroll
                    >
                      <h3>Активная тренировка: {currentWorkout.day}</h3>
                      <p className="subtitle">
                        Дата:{' '}
                        {formatWorkoutDateLabel(currentWorkout.date) || '—'} ·
                        можно сохранять черновик и править упражнения до
                        завершения.
                      </p>
                      <label>Название тренировки</label>
                      <input
                        value={currentWorkout.day || ''}
                        onChange={(e) =>
                          updateCurrentWorkoutField('day', e.target.value)
                        }
                        placeholder="Например: День ног"
                      />
                      <label>Дата тренировки</label>
                      <input
                        type="date"
                        value={currentWorkout.date || ''}
                        onChange={(e) =>
                          updateCurrentWorkoutField('date', e.target.value)
                        }
                      />
                      <label>Заметки</label>
                      <textarea
                        value={currentWorkout.notes || ''}
                        onChange={(e) =>
                          updateCurrentWorkoutField('notes', e.target.value)
                        }
                        rows={2}
                        placeholder="Опционально"
                      />
                      <div className="row">
                        <button
                          className="ghost-btn ghost-btn--emerald"
                          onClick={addExerciseToCurrentWorkout}
                        >
                          Добавить упражнение
                        </button>
                      </div>
                      <div className="users-table-wrap">
                        <table className="users-table">
                          <thead>
                            <tr>
                              <th>Упражнение</th>
                              <th>Комментарий</th>
                            </tr>
                          </thead>
                          <tbody>
                            {currentWorkout.exercises.length === 0 && (
                              <tr>
                                <td colSpan="2">
                                  Нет упражнений. Добавь первое упражнение.
                                </td>
                              </tr>
                            )}
                            {currentWorkout.exercises.flatMap(
                              (exercise, exIdx) => {
                                const exerciseInsights =
                                  exerciseHistoryInsights.get(
                                    normalizeExerciseName(exercise.name),
                                  );
                                const setsCollapsed = Boolean(
                                  activeWorkoutCollapsedExerciseIds[
                                    exercise.id
                                  ],
                                );
                                const setCount = exercise.sets?.length ?? 0;
                                const headRow = (
                                  <tr key={`${exercise.id}-head`}>
                                    <td colSpan="2">
                                      <div
                                        className="row row--inline"
                                        style={{ gap: '8px' }}
                                      >
                                        <div
                                          style={{
                                            flex: '1 1 min(100%, 200px)',
                                            minWidth: 0,
                                          }}
                                        >
                                          <b>{exercise.name || '—'}</b>
                                          {' · '}
                                          <span>{exercise.meta || '—'}</span>
                                          {exerciseInsights?.lastSession ? (
                                            <div className="workout-exercise-history-hints">
                                              <span>
                                                Прошлый раз (
                                                {formatWorkoutDateLabel(
                                                  exerciseInsights.lastSession
                                                    .date,
                                                ) || '—'}
                                                ):{' '}
                                                {formatWorkoutSetsSummary(
                                                  exerciseInsights.lastSession
                                                    .sets,
                                                )}
                                              </span>
                                            </div>
                                          ) : null}
                                          {setsCollapsed ? (
                                            <span
                                              className="subtitle"
                                              style={{ marginLeft: 8 }}
                                            >
                                              · {setCount}{' '}
                                              {setCount === 1
                                                ? 'подход'
                                                : setCount >= 2 && setCount <= 4
                                                  ? 'подхода'
                                                  : 'подходов'}
                                            </span>
                                          ) : null}
                                        </div>
                                        <div
                                          className="row row--inline row--inline-nowrap"
                                          style={{ flexShrink: 0 }}
                                        >
                                          <button
                                            type="button"
                                            className="ghost-btn"
                                            title={
                                              setsCollapsed
                                                ? 'Развернуть подходы'
                                                : 'Свернуть подходы'
                                            }
                                            aria-label={
                                              setsCollapsed
                                                ? 'Развернуть подходы'
                                                : 'Свернуть подходы'
                                            }
                                            aria-expanded={!setsCollapsed}
                                            onClick={() =>
                                              toggleActiveWorkoutExerciseCollapsed(
                                                exercise.id,
                                              )
                                            }
                                          >
                                            {setsCollapsed ? '▸' : '▾'}
                                          </button>
                                          <button
                                            type="button"
                                            className="ghost-btn"
                                            title="Выше"
                                            disabled={exIdx === 0}
                                            onClick={() =>
                                              moveCurrentWorkoutExercise(
                                                exercise.id,
                                                -1,
                                              )
                                            }
                                          >
                                            ↑
                                          </button>
                                          <button
                                            type="button"
                                            className="ghost-btn"
                                            title="Ниже"
                                            disabled={
                                              exIdx >=
                                              currentWorkout.exercises.length -
                                                1
                                            }
                                            onClick={() =>
                                              moveCurrentWorkoutExercise(
                                                exercise.id,
                                                1,
                                              )
                                            }
                                          >
                                            ↓
                                          </button>
                                        </div>
                                      </div>
                                    </td>
                                  </tr>
                                );
                                if (setsCollapsed) return [headRow];
                                const setsRow = (
                                  <tr key={`${exercise.id}-sets`}>
                                    <td colSpan="2">
                                      <div className="workout-sets">
                                        {exercise.sets.map(
                                          (setItem, setIdx) => {
                                            const lastSet =
                                              exerciseInsights?.lastSession
                                                ?.sets?.[setIdx];
                                            const lastSetHint =
                                              formatWorkoutSetPair(lastSet);
                                            return (
                                            <div
                                              key={`${exercise.id}-active-set-${setIdx}`}
                                              className="row workout-set-row"
                                            >
                                              <div className="workout-set-index-col">
                                                <span>{setIdx + 1}</span>
                                                {lastSetHint ? (
                                                  <span
                                                    className="workout-set-prev"
                                                    title="Прошлый раз"
                                                  >
                                                    {lastSetHint}
                                                  </span>
                                                ) : null}
                                              </div>
                                              <div className="workout-set-weight-control">
                                                <input
                                                  type="number"
                                                  inputMode="decimal"
                                                  min="0"
                                                  step="0.5"
                                                  value={setItem.weight}
                                                  onChange={(e) =>
                                                    updateCurrentWorkoutSet(
                                                      exercise.id,
                                                      setIdx,
                                                      'weight',
                                                      e.target.value,
                                                    )
                                                  }
                                                  placeholder="Вес"
                                                />
                                                <div className="workout-set-weight-steps">
                                                  <button
                                                    type="button"
                                                    className="ghost-btn"
                                                    onClick={() =>
                                                      adjustCurrentWorkoutSetWeight(
                                                        exercise.id,
                                                        setIdx,
                                                        -5,
                                                      )
                                                    }
                                                  >
                                                    -5
                                                  </button>
                                                  <button
                                                    type="button"
                                                    className="ghost-btn"
                                                    onClick={() =>
                                                      adjustCurrentWorkoutSetWeight(
                                                        exercise.id,
                                                        setIdx,
                                                        -2.5,
                                                      )
                                                    }
                                                  >
                                                    -2,5
                                                  </button>
                                                  <button
                                                    type="button"
                                                    className="ghost-btn"
                                                    onClick={() =>
                                                      adjustCurrentWorkoutSetWeight(
                                                        exercise.id,
                                                        setIdx,
                                                        2.5,
                                                      )
                                                    }
                                                  >
                                                    +2,5
                                                  </button>
                                                  <button
                                                    type="button"
                                                    className="ghost-btn"
                                                    onClick={() =>
                                                      adjustCurrentWorkoutSetWeight(
                                                        exercise.id,
                                                        setIdx,
                                                        5,
                                                      )
                                                    }
                                                  >
                                                    +5
                                                  </button>
                                                </div>
                                              </div>
                                              <div className="workout-set-weight-control">
                                                <input
                                                  type="number"
                                                  inputMode="numeric"
                                                  min="0"
                                                  step="1"
                                                  value={setItem.reps}
                                                  onChange={(e) =>
                                                    updateCurrentWorkoutSet(
                                                      exercise.id,
                                                      setIdx,
                                                      'reps',
                                                      e.target.value,
                                                    )
                                                  }
                                                  placeholder="Повт."
                                                />
                                                <div className="workout-set-weight-steps">
                                                  <button
                                                    type="button"
                                                    className="ghost-btn"
                                                    onClick={() =>
                                                      adjustCurrentWorkoutSetReps(
                                                        exercise.id,
                                                        setIdx,
                                                        -2,
                                                      )
                                                    }
                                                  >
                                                    -2
                                                  </button>
                                                  <button
                                                    type="button"
                                                    className="ghost-btn"
                                                    onClick={() =>
                                                      adjustCurrentWorkoutSetReps(
                                                        exercise.id,
                                                        setIdx,
                                                        -1,
                                                      )
                                                    }
                                                  >
                                                    -1
                                                  </button>
                                                  <button
                                                    type="button"
                                                    className="ghost-btn"
                                                    onClick={() =>
                                                      adjustCurrentWorkoutSetReps(
                                                        exercise.id,
                                                        setIdx,
                                                        1,
                                                      )
                                                    }
                                                  >
                                                    +1
                                                  </button>
                                                  <button
                                                    type="button"
                                                    className="ghost-btn"
                                                    onClick={() =>
                                                      adjustCurrentWorkoutSetReps(
                                                        exercise.id,
                                                        setIdx,
                                                        2,
                                                      )
                                                    }
                                                  >
                                                    +2
                                                  </button>
                                                </div>
                                              </div>
                                              <div className="workout-set-weight-control">
                                                <select
                                                  value={setItem.rpe || '8'}
                                                  onChange={(e) =>
                                                    updateCurrentWorkoutSet(
                                                      exercise.id,
                                                      setIdx,
                                                      'rpe',
                                                      e.target.value,
                                                    )
                                                  }
                                                >
                                                  <option value="6">6</option>
                                                  <option value="7">7</option>
                                                  <option value="8">8</option>
                                                  <option value="9">9</option>
                                                  <option value="10">10</option>
                                                </select>
                                                <div className="workout-set-weight-steps">
                                                  <button
                                                    type="button"
                                                    className="ghost-btn"
                                                    onClick={() =>
                                                      adjustCurrentWorkoutSetRpe(
                                                        exercise.id,
                                                        setIdx,
                                                        -1,
                                                      )
                                                    }
                                                  >
                                                    -
                                                  </button>
                                                  <button
                                                    type="button"
                                                    className="ghost-btn"
                                                    onClick={() =>
                                                      adjustCurrentWorkoutSetRpe(
                                                        exercise.id,
                                                        setIdx,
                                                        1,
                                                      )
                                                    }
                                                  >
                                                    +
                                                  </button>
                                                </div>
                                              </div>
                                              <button
                                                className="danger-btn danger-btn--icon"
                                                onClick={() =>
                                                  removeCurrentWorkoutSet(
                                                    exercise.id,
                                                    setIdx,
                                                  )
                                                }
                                                disabled={
                                                  exercise.sets.length <= 1
                                                }
                                              >
                                                ×
                                              </button>
                                            </div>
                                            );
                                          },
                                        )}
                                      </div>
                                      <div className="row">
                                        <button
                                          className="ghost-btn ghost-btn--emerald"
                                          onClick={() =>
                                            addCurrentWorkoutSet(exercise.id)
                                          }
                                        >
                                          +
                                        </button>
                                        <button
                                          className="danger-btn danger-btn--icon"
                                          onClick={() =>
                                            openDeleteCurrentWorkoutExerciseModal(
                                              exercise.id,
                                            )
                                          }
                                          title="Удалить"
                                        >
                                          ×
                                        </button>
                                      </div>
                                    </td>
                                  </tr>
                                );
                                return [headRow, setsRow];
                              },
                            )}
                          </tbody>
                        </table>
                      </div>
                      <div className="row">
                        <button
                          type="button"
                          onClick={() => persistCurrentWorkout(false)}
                        >
                          Сохранить черновик
                        </button>
                        <button
                          type="button"
                          className="ghost-btn"
                          onClick={() => persistCurrentWorkout(true)}
                        >
                          Завершить тренировку
                        </button>
                        <button
                          type="button"
                          className="ghost-btn"
                          onClick={hideActiveWorkoutModal}
                        >
                          Закрыть
                        </button>
                      </div>
                    </ModalShell>
                  )}
                {tab === 'workouts' &&
                  workoutsSubTab === 'strength' &&
                  strengthSubTab === 'history' &&
                  selectedWorkoutHistorySession && (
                    <ModalShell
                      open={Boolean(selectedWorkoutHistorySession)}
                      onClose={closeWorkoutHistoryModal}
                      wide
                      scroll
                    >
                      <h3>
                        {selectedWorkoutHistorySession.day || 'Тренировка'}
                      </h3>
                      <p className="subtitle">
                        Дата:{' '}
                        {formatWorkoutDateLabel(
                          selectedWorkoutHistorySession.date ||
                            selectedWorkoutHistorySession._date,
                        ) || '—'}
                      </p>
                      <p className="subtitle">
                        Заметки: {selectedWorkoutHistorySession.notes || '—'}
                      </p>
                      <div className="users-table-wrap">
                        <table className="users-table">
                          <thead>
                            <tr>
                              <th>Упражнение</th>
                              {Array.from(
                                { length: historyMaxSetCount },
                                (_, idx) => (
                                  <th
                                    key={`history-set-col-${idx}`}
                                  >{`Подход ${idx + 1}`}</th>
                                ),
                              )}
                            </tr>
                          </thead>
                          <tbody>
                            {(selectedWorkoutHistorySession.exercises || [])
                              .length === 0 && (
                              <tr>
                                <td colSpan={historyMaxSetCount + 1}>
                                  В этой тренировке нет упражнений.
                                </td>
                              </tr>
                            )}
                            {(
                              selectedWorkoutHistorySession.exercises || []
                            ).map((exercise) => (
                              <tr key={exercise.id}>
                                <td>{exercise.name || '-'}</td>
                                {Array.from(
                                  { length: historyMaxSetCount },
                                  (_, idx) => {
                                    const setItem = (exercise.sets || [])[idx];
                                    return (
                                      <td key={`${exercise.id}-set-${idx}`}>
                                        {setItem
                                          ? `${setItem.weight || '-'} кг × ${setItem.reps || '-'} (RPE ${setItem.rpe || '-'})`
                                          : '—'}
                                      </td>
                                    );
                                  },
                                )}
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                      <div className="row">
                        <button
                          className="ghost-btn"
                          onClick={closeWorkoutHistoryModal}
                        >
                          Закрыть
                        </button>
                      </div>
                    </ModalShell>
                  )}

                {tab === 'admin' &&
                  isAdmin &&
                  adminDialogModalKind === 'create' && (
                    <ModalShell
                      open={adminDialogModalKind === 'create'}
                      onClose={() => {
                        setAdminDialogModalKind(null);
                        setAdminDialogTitleDraft('');
                      }}
                    >
                      <h3>Новый диалог</h3>
                      <label>Название</label>
                      <input
                        value={adminDialogTitleDraft}
                        onChange={(e) =>
                          setAdminDialogTitleDraft(e.target.value)
                        }
                        placeholder="Новый диалог"
                      />
                      <div className="row">
                        <button onClick={submitCreateAdminDialog}>
                          Создать
                        </button>
                        <button
                          className="ghost-btn"
                          onClick={() => {
                            setAdminDialogModalKind(null);
                            setAdminDialogTitleDraft('');
                          }}
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}
                {tab === 'admin' &&
                  isAdmin &&
                  adminDialogModalKind === 'rename' && (
                    <ModalShell
                      open={adminDialogModalKind === 'rename'}
                      onClose={() => {
                        setAdminDialogModalKind(null);
                        setAdminDialogTitleDraft('');
                      }}
                    >
                      <h3>Переименовать диалог</h3>
                      <label>Название</label>
                      <input
                        value={adminDialogTitleDraft}
                        onChange={(e) =>
                          setAdminDialogTitleDraft(e.target.value)
                        }
                        placeholder="Новый диалог"
                      />
                      <div className="row">
                        <button onClick={submitRenameAdminDialog}>
                          Сохранить
                        </button>
                        <button
                          className="ghost-btn"
                          onClick={() => {
                            setAdminDialogModalKind(null);
                            setAdminDialogTitleDraft('');
                          }}
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}
                {tab === 'admin' &&
                  isAdmin &&
                  adminDialogModalKind === 'delete' && (
                    <ModalShell
                      open={adminDialogModalKind === 'delete'}
                      onClose={() => setAdminDialogModalKind(null)}
                    >
                      <h3>Удалить диалог</h3>
                      <p className="subtitle">
                        Удалить выбранный диалог без восстановления?
                      </p>
                      <div className="row">
                        <button
                          className="danger-btn danger-btn--icon"
                          onClick={submitDeleteAdminDialog}
                          title="Удалить"
                        >
                          ×
                        </button>
                        <button
                          className="ghost-btn"
                          onClick={() => setAdminDialogModalKind(null)}
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}

                {tab === 'workouts' &&
                  workoutsSubTab === 'strength' &&
                  strengthSubTab === 'manage' &&
                  workoutsManageSubTab === 'exercises' &&
                  pendingDeleteCatalogExerciseId && (
                    <ModalShell
                      open={Boolean(pendingDeleteCatalogExerciseId)}
                      onClose={() => setPendingDeleteCatalogExerciseId(null)}
                    >
                      <h3>Удалить упражнение</h3>
                      <p className="subtitle">
                        Удалить «
                        {workoutExerciseCatalog.find(
                          (x) =>
                            String(x.id) ===
                            String(pendingDeleteCatalogExerciseId),
                        )?.name || 'упражнение'}
                        » из каталога?
                      </p>
                      <div className="row">
                        <button
                          className="danger-btn danger-btn--icon"
                          onClick={confirmDeleteCatalogExercise}
                          title="Удалить"
                        >
                          ×
                        </button>
                        <button
                          className="ghost-btn"
                          onClick={() =>
                            setPendingDeleteCatalogExerciseId(null)
                          }
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}

                {tab === 'workouts' &&
                  workoutsSubTab === 'strength' &&
                  strengthSubTab === 'history' &&
                  pendingDeleteWorkoutSessionId && (
                    <ModalShell
                      open={Boolean(pendingDeleteWorkoutSessionId)}
                      onClose={() => setPendingDeleteWorkoutSessionId(null)}
                    >
                      <h3>Удалить тренировку</h3>
                      <p className="subtitle">
                        Удалить «
                        {historyWorkoutLogs.find(
                          (x) => x.id === pendingDeleteWorkoutSessionId,
                        )?.day || 'тренировку'}
                        » из истории?
                      </p>
                      <div className="row">
                        <button
                          className="danger-btn danger-btn--icon"
                          onClick={confirmDeleteWorkoutSession}
                          title="Удалить"
                        >
                          ×
                        </button>
                        <button
                          className="ghost-btn"
                          onClick={() => setPendingDeleteWorkoutSessionId(null)}
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}

                {tab === 'workouts' &&
                  tab === 'bike' &&
                  pendingDeleteBikeActivityId && (
                    <ModalShell
                      open
                      onClose={() => setPendingDeleteBikeActivityId(null)}
                    >
                      <h3>Удалить велотренировку?</h3>
                      <p className="subtitle">
                        Запись и все точки трека будут удалены из базы.
                      </p>
                      <div className="row">
                        <button
                          type="button"
                          className="danger-btn"
                          onClick={deleteBikeActivityConfirmed}
                        >
                          Удалить
                        </button>
                        <button
                          type="button"
                          className="ghost-btn"
                          onClick={() => setPendingDeleteBikeActivityId(null)}
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}

                {tab === 'workouts' &&
                  tab === 'bike' &&
                  bikeDetailOpen && (
                    <ModalShell
                      open
                      wide
                      titleId="bike-detail-title"
                      onClose={() => {
                        setBikeDetailOpen(false);
                        setBikeDetail(null);
                      }}
                    >
                      <h3 id="bike-detail-title">Маршрут</h3>
                      {bikeDetailLoading ? (
                        <p className="subtitle">Загрузка…</p>
                      ) : bikeDetail ? (
                        <>
                          <p className="subtitle">
                            {bikeDetail.notes || bikeDetail.sport} ·{' '}
                            {formatUtcDateTime(bikeDetail.startTimeUtc)}
                            {bikeDetail.trackpointCount >
                            (bikeDetail.trackpoints?.length || 0)
                              ? ` · на карте ${bikeDetail.trackpoints?.length || 0} из ${bikeDetail.trackpointCount} точек`
                              : bikeDetail.trackpointCount > 0
                                ? ` · ${bikeDetail.trackpointCount} точек`
                                : null}
                          </p>
                          <BikeTrackMap
                            trackpoints={bikeDetail.trackpoints}
                            active={bikeDetailOpen && !bikeDetailLoading}
                          />
                        </>
                      ) : (
                        <p className="subtitle">Нет данных.</p>
                      )}
                      <div className="row">
                        <button
                          type="button"
                          className="ghost-btn"
                          onClick={() => {
                            setBikeDetailOpen(false);
                            setBikeDetail(null);
                          }}
                        >
                          Закрыть
                        </button>
                      </div>
                    </ModalShell>
                  )}

                {tab === 'workouts' &&
                  workoutsSubTab === 'strength' &&
                  strengthSubTab === 'my-workout' &&
                  pendingDeleteCurrentWorkoutExerciseId &&
                  currentWorkout && (
                    <ModalShell
                      open={Boolean(pendingDeleteCurrentWorkoutExerciseId)}
                      onClose={() =>
                        setPendingDeleteCurrentWorkoutExerciseId(null)
                      }
                    >
                      <h3>Удалить упражнение</h3>
                      <p className="subtitle">
                        Удалить «
                        {currentWorkout.exercises.find(
                          (x) => x.id === pendingDeleteCurrentWorkoutExerciseId,
                        )?.name || 'упражнение'}
                        » из текущей тренировки?
                      </p>
                      <div className="row">
                        <button
                          className="danger-btn"
                          onClick={confirmDeleteCurrentWorkoutExercise}
                          title="Удалить"
                        >
                          Удалить
                        </button>
                        <button
                          className="ghost-btn"
                          onClick={() =>
                            setPendingDeleteCurrentWorkoutExerciseId(null)
                          }
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}

                {tab === 'workouts' &&
                  workoutsSubTab === 'strength' &&
                  strengthSubTab === 'manage' &&
                  workoutsManageSubTab === 'exercises' &&
                  isCreateExerciseModalOpen && (
                    <ModalShell
                      open={isCreateExerciseModalOpen}
                      onClose={closeCatalogExerciseModal}
                    >
                      <h3>
                        {editingCatalogExerciseId
                          ? 'Редактировать упражнение'
                          : 'Создать упражнение'}
                      </h3>
                      <label>Название упражнения</label>
                      <input
                        value={newCatalogExerciseName}
                        onChange={(e) =>
                          setNewCatalogExerciseName(e.target.value)
                        }
                        placeholder="Например: Жим лежа"
                      />
                      <label>Группа мышц (опционально)</label>
                      <input
                        list="exercise-muscle-groups"
                        value={newCatalogExerciseMuscleGroup}
                        onChange={(e) =>
                          setNewCatalogExerciseMuscleGroup(e.target.value)
                        }
                        placeholder="Например: Спина"
                      />
                      <datalist id="exercise-muscle-groups">
                        {EXERCISE_MUSCLE_GROUP_OPTIONS.map((group) => (
                          <option key={group} value={group} />
                        ))}
                      </datalist>
                      <label>Комментарий (опционально)</label>
                      <input
                        value={newCatalogExerciseMeta}
                        onChange={(e) =>
                          setNewCatalogExerciseMeta(e.target.value)
                        }
                        placeholder="Например: гриф + 20 кг"
                      />
                      <div className="row">
                        <button onClick={saveCatalogExercise}>
                          {editingCatalogExerciseId ? 'Сохранить' : 'Создать'}
                        </button>
                        <button
                          className="ghost-btn"
                          onClick={closeCatalogExerciseModal}
                        >
                          Отмена
                        </button>
                      </div>
                    </ModalShell>
                  )}
                </main>
              </div>
            )
          }
        />
      </Routes>
    </>
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
