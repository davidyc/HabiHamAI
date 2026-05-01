function TopNav({ tab, currentUserName, isAdmin, hasAiAccess, onTabChange, onLogout }) {
  const tabs = [
    ...(hasAiAccess ? ["ai"] : []),
    "workouts",
    "profile",
    ...(isAdmin ? ["admin"] : [])
  ];

  return (
    <div className="top-nav">
      <div className="top-nav-left">
        <h1>HabiHamAI</h1>
        <span className="top-nav-subtitle">Рабочая зона</span>
      </div>
      <nav className="top-nav-center">
        {tabs.map((id) => (
          <button
            key={id}
            className={`top-nav-tab ${tab === id ? "active" : ""}`}
            onClick={() => onTabChange(id)}
          >
            {id === "ai" ? "AI Chat" : id === "workouts" ? "Workouts" : id === "profile" ? "Profile" : "Admin"}
          </button>
        ))}
      </nav>
      <div className="top-nav-right">
        <span className="user-chip">Пользователь: {currentUserName}</span>
        <button className="logout-btn" onClick={onLogout}>
          Logout
        </button>
      </div>
    </div>
  );
}

export default TopNav;
