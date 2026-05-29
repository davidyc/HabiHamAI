import { useEffect, useRef, useState } from "react";

function TopNav({ tab, currentUserName, isAdmin, hasAiAccess, onTabChange, onLogout }) {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const topbarRef = useRef(null);
  const sidebarTabs = ["workouts", "progress", "tracking"];
  const topbarTabs = [
    "profile",
    ...(hasAiAccess ? ["ai"] : []),
    ...(isAdmin ? ["admin"] : [])
  ];

  useEffect(() => {
    const topbar = topbarRef.current;
    if (!topbar) return undefined;

    const syncTopbarHeight = () => {
      const { height } = topbar.getBoundingClientRect();
      document.documentElement.style.setProperty(
        "--dashboard-mobile-topbar-height",
        `${Math.ceil(height)}px`
      );
    };

    syncTopbarHeight();

    const observer = new ResizeObserver(syncTopbarHeight);
    observer.observe(topbar);
    window.addEventListener("resize", syncTopbarHeight);

    return () => {
      observer.disconnect();
      window.removeEventListener("resize", syncTopbarHeight);
      document.documentElement.style.removeProperty("--dashboard-mobile-topbar-height");
    };
  }, []);

  useEffect(() => {
    if (!isSidebarOpen) return undefined;

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const onKeyDown = (event) => {
      if (event.key === "Escape") {
        setIsSidebarOpen(false);
      }
    };

    window.addEventListener("keydown", onKeyDown);
    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [isSidebarOpen]);

  function handleSidebarTabChange(id) {
    onTabChange(id);
    setIsSidebarOpen(false);
  }

  return (
    <>
      <header ref={topbarRef} className="dashboard-topbar">
        <div className="dashboard-topbar-left">
          <button
            type="button"
            className="sidebar-toggle"
            aria-label={isSidebarOpen ? "Закрыть меню" : "Открыть меню"}
            aria-expanded={isSidebarOpen}
            aria-controls="dashboard-sidebar"
            onClick={() => setIsSidebarOpen((open) => !open)}
          >
            <span className="sidebar-toggle-icon" aria-hidden="true" />
          </button>
          <div className="dashboard-topbar-brand">HabiHamAI</div>
        </div>
        <nav className="dashboard-topbar-links" aria-label="Навигация в верхней панели">
          {topbarTabs.map((id) => (
            <button
              key={id}
              type="button"
              className={`topbar-nav-item ${tab === id ? "active" : ""}`}
              onClick={() => onTabChange(id)}
            >
              {id === "ai"
                ? "AI помощник"
                : id === "profile"
                  ? "Профиль"
                  : id === "progress"
                    ? "Мой прогресс"
                    : "Админ"}
            </button>
          ))}
        </nav>
        <div className="topbar-right">
          <div className="topbar-user">
            <div className="sidebar-avatar topbar-avatar">{(currentUserName || "U").charAt(0).toUpperCase()}</div>
            <div className="topbar-user-meta">
              <strong>{currentUserName}</strong>
              <span>Активный профиль</span>
            </div>
          </div>
          <button type="button" className="logout-btn topbar-logout-btn" onClick={onLogout}>
            Выход
          </button>
        </div>
      </header>

      <div
        className={`sidebar-backdrop ${isSidebarOpen ? "visible" : ""}`}
        role="presentation"
        aria-hidden={!isSidebarOpen}
        onClick={() => setIsSidebarOpen(false)}
      />

      <aside
        id="dashboard-sidebar"
        className={`left-sidebar ${isSidebarOpen ? "is-open" : ""}`}
        aria-label="Боковая навигация"
        aria-hidden={!isSidebarOpen}
      >
        <div className="sidebar-drawer-head">
          <span className="sidebar-drawer-title">Меню</span>
          <button
            type="button"
            className="sidebar-close"
            aria-label="Закрыть меню"
            onClick={() => setIsSidebarOpen(false)}
          >
            ×
          </button>
        </div>
        <nav className="sidebar-nav">
          {sidebarTabs.map((id) => (
            <button
              key={id}
              type="button"
              className={`sidebar-nav-item ${tab === id ? "active" : ""}`}
              onClick={() => handleSidebarTabChange(id)}
            >
              {id === "workouts"
                ? "Тренировки"
                : id === "progress"
                  ? "Мой прогресс"
                  : id === "tracking"
                    ? "Трекинг"
                    : id}
            </button>
          ))}
        </nav>
      </aside>
    </>
  );
}

export default TopNav;
