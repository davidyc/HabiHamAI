function TopNav({ tab, currentUserName, isAdmin, hasAiAccess, onTabChange, onLogout }) {
  const tabs = [
    ...(hasAiAccess ? ["ai"] : []),
    "workouts",
    "profile",
    ...(isAdmin ? ["admin"] : [])
  ];

  return (
    <>
      <header className="dashboard-topbar">
        <div className="dashboard-topbar-brand">HabiHamAI</div>
      </header>

      <aside className="left-sidebar" aria-label="Sidebar navigation">
        <div className="sidebar-user">
          <div className="sidebar-avatar">{(currentUserName || "U").charAt(0).toUpperCase()}</div>
          <div className="sidebar-user-meta">
            <strong>{currentUserName}</strong>
            <button
              type="button"
              className="sidebar-profile-link"
              onClick={() => onTabChange("profile")}
            >
              Активный профиль
            </button>
          </div>
        </div>

        <nav className="sidebar-nav">
          {tabs.map((id) => (
            <button
              key={id}
              className={`sidebar-nav-item ${tab === id ? "active" : ""}`}
              onClick={() => onTabChange(id)}
            >
              {id === "ai" ? "AI чат" : id === "workouts" ? "Тренировки" : id === "profile" ? "Профиль" : "Админ"}
            </button>
          ))}
        </nav>

        <div className="sidebar-footer">
          <button className="logout-btn" onClick={onLogout}>
            Выход
          </button>
        </div>
      </aside>
    </>
  );
}

export default TopNav;
