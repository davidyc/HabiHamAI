import { useEffect, useRef, useState } from "react";
import { SegmentTab, SegmentTabs } from "./shared/ui/SegmentTabs";

function TopNav({
  tab,
  currentUserName,
  sidebarTabs = [],
  accountMenuItems = [],
  onTabChange,
  onLogout,
}) {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
  const topbarRef = useRef(null);
  const userMenuRef = useRef(null);

  useEffect(() => {
    const topbar = topbarRef.current;
    if (!topbar) return undefined;

    const syncTopbarHeight = () => {
      const { height } = topbar.getBoundingClientRect();
      document.documentElement.style.setProperty(
        "--dashboard-mobile-topbar-height",
        `${Math.ceil(height)}px`,
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
  }, [isUserMenuOpen]);

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

  useEffect(() => {
    if (!isUserMenuOpen) return undefined;

    const onPointerDown = (event) => {
      if (!userMenuRef.current?.contains(event.target)) {
        setIsUserMenuOpen(false);
      }
    };
    const onKeyDown = (event) => {
      if (event.key === "Escape") {
        setIsUserMenuOpen(false);
      }
    };

    document.addEventListener("mousedown", onPointerDown);
    window.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("mousedown", onPointerDown);
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [isUserMenuOpen]);

  function handleSidebarTabChange(id) {
    onTabChange(id);
    setIsSidebarOpen(false);
  }

  function handleAccountNav(id) {
    onTabChange(id);
    setIsUserMenuOpen(false);
  }

  function handleLogout() {
    setIsUserMenuOpen(false);
    onLogout();
  }

  function accountMenuItemClass(id, isActive) {
    return `topbar-user-menu__item topbar-user-menu__item--${id}${isActive ? " is-active" : ""}`;
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

        <div className="topbar-right" ref={userMenuRef}>
          <button
            type="button"
            className={`topbar-user-menu-trigger${isUserMenuOpen ? " is-open" : ""}`}
            aria-haspopup="menu"
            aria-expanded={isUserMenuOpen}
            aria-controls="topbar-user-menu"
            onClick={() => setIsUserMenuOpen((open) => !open)}
          >
            <span className="topbar-user">
              <span className="sidebar-avatar topbar-avatar">
                {(currentUserName || "U").charAt(0).toUpperCase()}
              </span>
              <span className="topbar-user-meta">
                <strong>{currentUserName}</strong>
                <span>Активный профиль</span>
              </span>
            </span>
            <span className="topbar-user-menu-chevron" aria-hidden="true" />
          </button>
          {isUserMenuOpen ? (
            <ul
              id="topbar-user-menu"
              className="topbar-user-menu"
              role="menu"
              aria-label="Меню аккаунта"
            >
              {accountMenuItems.map(({ id, label }) => (
                <li key={id} role="none">
                  <button
                    type="button"
                    role="menuitem"
                    className={accountMenuItemClass(id, tab === id)}
                    onClick={() => handleAccountNav(id)}
                  >
                    {label}
                  </button>
                </li>
              ))}
              <li className="topbar-user-menu__divider" role="separator" />
              <li role="none">
                <button
                  type="button"
                  role="menuitem"
                  className="topbar-user-menu__item topbar-user-menu__item--logout"
                  onClick={handleLogout}
                >
                  Выход
                </button>
              </li>
            </ul>
          ) : null}
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
        <SegmentTabs
          variant="sidebar"
          className="sidebar-nav"
          ariaLabel="Основные разделы"
        >
          {sidebarTabs.map(({ id, label }) => (
            <SegmentTab
              key={id}
              active={tab === id}
              onClick={() => handleSidebarTabChange(id)}
            >
              {label}
            </SegmentTab>
          ))}
        </SegmentTabs>
      </aside>
    </>
  );
}

export default TopNav;
