import { NavLink, Outlet } from "react-router-dom";

const navItems = [
  { to: "/", label: "Dashboard", icon: "query_stats", end: true },
  { to: "/new", label: "New Run", icon: "add_circle", end: false },
];

export function Layout() {
  return (
    <div className="min-h-screen bg-surface">
      <aside className="fixed left-0 top-0 z-50 flex h-screen w-64 select-none flex-col justify-between bg-surface-container-low">
        <div className="flex flex-col">
          <div className="flex h-12 items-center gap-2 px-4">
            <span className="material-symbols-outlined text-primary">biotech</span>
            <div className="flex flex-col leading-none">
              <span className="text-sm font-semibold tracking-tight text-on-surface">Bio Pipeline</span>
              <span className="mt-0.5 font-mono text-[11px] tracking-wide text-primary">Dashboard</span>
            </div>
          </div>

          <nav className="space-y-0.5 px-2 py-2">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  `flex items-center gap-2 rounded px-3 py-2 text-sm transition-colors ${
                    isActive
                      ? "bg-primary-container font-semibold text-on-primary-container"
                      : "text-on-surface-variant hover:bg-surface-container hover:text-on-surface"
                  }`
                }
              >
                <span className="material-symbols-outlined text-base">{item.icon}</span>
                <span>{item.label}</span>
              </NavLink>
            ))}
          </nav>
        </div>

        <div className="space-y-2 p-3">
          <div className="space-y-1 rounded bg-surface-container-lowest p-3 font-mono text-[11px] text-on-surface-variant">
            <p>Portfolio / learning project.</p>
            <p>
              Real Nextflow + Docker pipeline running on small,{" "}
              <span className="text-on-surface">synthetic</span> sample DNA data.
            </p>
          </div>
        </div>
      </aside>

      <div className="flex min-h-screen flex-col pl-64">
        <header className="sticky top-0 z-40 flex h-12 items-center justify-between border-b border-surface-container-high bg-surface/90 px-6 backdrop-blur-xl">
          <div className="flex items-center gap-2 rounded bg-surface-container px-2 py-1 font-mono text-xs text-on-surface-variant">
            <span className="material-symbols-outlined text-xs text-primary">dns</span>
            <span>toy_chromosome_1</span>
            <span className="text-outline">|</span>
            <span>3-step Nextflow pipeline</span>
          </div>
        </header>

        <main className="flex-1">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
