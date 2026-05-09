function TopNav({ tab, currentUserName, isAdmin, hasAiAccess, onTabChange, onLogout }) {
  const sidebarTabs = ["workouts"];
  const topbarTabs = [
    "profile",
    ...(hasAiAccess ? ["ai"] : []),
    ...(isAdmin ? ["admin"] : [])
  ];

  return (
    <>
      <header className="dashboard-topbar">
        <div className="dashboard-topbar-brand">HabiHamAI</div>
        <nav className="dashboard-topbar-links" aria-label="Навигация в верхней панели">
          {topbarTabs.map((id) => (
            <button
              key={id}
              type="button"
              className={`topbar-nav-item ${tab === id ? "active" : ""}`}
              onClick={() => onTabChange(id)}
            >
              {id === "ai" ? "AI помощник" : id === "profile" ? "Профиль" : "Админ"}
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

      <aside className="left-sidebar" aria-label="Боковая навигация">
        <nav className="sidebar-nav">
          {sidebarTabs.map((id) => (
            <button
              key={id}
              className={`sidebar-nav-item ${tab === id ? "active" : ""}`}
              onClick={() => onTabChange(id)}
            >
              {id === "workouts" ? "Тренировки" : id}
            </button>
          ))}
        </nav>

      </aside>
    </>
  );
}

export default TopNav;
