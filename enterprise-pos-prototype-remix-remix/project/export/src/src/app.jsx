// Main app shell v2 — R Technologies POS branding
// Auth flow: account-login → pin-login → shift-open → checkout
// Lock icon → pin-login, logout → account-login

const NAV = [
  { id: "checkout", label: "Checkout",  icon: "cart",    section: "POS" },
  { id: "orders",   label: "Orders",    icon: "receipt", section: "POS" },
  { id: "shift",    label: "Shift",     icon: "shift",   section: "POS" },
  { id: "cash",     label: "Cash",      icon: "cash",    section: "Operations" },
  { id: "reports",  label: "Reports",   icon: "chart",   section: "Operations" },
  { id: "items",    label: "Items",     icon: "catalog", section: "Management" },
  { id: "people",   label: "People",    icon: "user",    section: "Management" },
  { id: "admin",    label: "Admin",     icon: "admin",   section: "Management" },
];

const SIDEBAR_W = 212;
const STATUSBAR_H = 52;
const HWSTRIP_H = 36;

// ── Hardware status strip ─────────────────────────────────────────────────────
const HardwareStrip = () => {
  const devices = [
    { id: "printer",   label: "Printer",   icon: "print",    status: "ready",   detail: "Epson TM-T88VII" },
    { id: "drawer",    label: "Drawer",    icon: "drawer",   status: "ready",   detail: "Ready · Last kick 14:38" },
    { id: "scanner",   label: "Scanner",   icon: "scan",     status: "ready",   detail: "Honeywell MS9590" },
    { id: "payment",   label: "Card terminal", icon: "terminal", status: "ready", detail: "Ingenico iPP320" },
    { id: "sync",      label: "Sync",      icon: "sync",     status: "synced",  detail: "12s ago · 0 pending" },
    { id: "network",   label: "Network",   icon: "offline",  status: "online",  detail: "100 Mbps · LAN" },
  ];
  const toneMap = { ready: "success", synced: "success", online: "success", warning: "amber", error: "danger" };
  return (
    <div style={{
      height: HWSTRIP_H, background: 'var(--navy-900)',
      display: 'flex', alignItems: 'center', padding: '0 18px', gap: 2, flexShrink: 0,
      borderBottom: '1px solid rgba(255,255,255,.06)',
      overflow: 'hidden',
    }}>
      <div style={{ fontSize: 10, color: 'rgba(255,255,255,.35)', textTransform: 'uppercase', letterSpacing: '.08em', fontWeight: 500, marginRight: 14, whiteSpace: 'nowrap' }}>Hardware</div>
      <div style={{ display: 'flex', gap: 4, flex: 1, overflowX: 'auto' }}>
        {devices.map(d => (
          <div key={d.id} title={d.detail} style={{
            display: 'inline-flex', alignItems: 'center', gap: 6, padding: '4px 10px', borderRadius: 6,
            background: 'rgba(255,255,255,.05)',
            cursor: 'default', whiteSpace: 'nowrap',
          }}>
            <Dot tone={toneMap[d.status]} size={6}/>
            <Icon name={d.icon} size={13} color="rgba(255,255,255,.55)"/>
            <span style={{ fontSize: 11.5, color: 'rgba(255,255,255,.65)', fontWeight: 400 }}>{d.label}</span>
          </div>
        ))}
      </div>
      <div style={{ marginLeft: 12, display: 'flex', alignItems: 'center', gap: 6, fontSize: 11, color: 'rgba(255,255,255,.35)' }}>
        <span className="num">v8.4.2</span>
        <span>·</span>
        <span>SQLite ✓</span>
        <span>·</span>
        <span>CatalogVersion 14</span>
      </div>
    </div>
  );
};

// ── Sidebar ───────────────────────────────────────────────────────────────────
const Sidebar = ({ active, onNav }) => {
  const sections = ["POS", "Operations", "Management"];
  const isActive = (n) =>
    active === n.id
    || (n.id === "checkout" && ["payment","receipt"].includes(active))
    || (n.id === "cash"     && ["drawer-to-vault","vault-to-bank"].includes(active))
    || (n.id === "shift"    && active === "shift-close")
    || (n.id === "items"    && active === "catalog");

  return (
    <div style={{
      width: SIDEBAR_W, height: '100%', background: 'var(--navy-900)', color: '#fff',
      display: 'flex', flexDirection: 'column', borderRight: '1px solid rgba(255,255,255,.05)',
      flexShrink: 0,
    }}>
      {/* Logo area */}
      <div style={{ padding: '16px 14px 12px', borderBottom: '1px solid rgba(255,255,255,.08)' }}>
        <img src=(window.__resources && window.__resources.logo) || "assets/logo.png" alt="imogyn Technologies" style={{ width: '100%', maxWidth: 160, filter: 'brightness(1.05)' }}/>
        <div style={{ marginTop: 8, fontSize: 10.5, color: 'rgba(255,255,255,.38)', letterSpacing: '.05em' }}>Enterprise POS · POS-01</div>
      </div>

      {/* Nav */}
      <div style={{ flex: 1, overflow: 'auto', padding: '12px 8px' }}>
        {sections.map(s => (
          <div key={s} style={{ marginBottom: 12 }}>
            <div style={{ padding: '2px 8px 7px', fontSize: 9.5, color: 'rgba(255,255,255,.35)', letterSpacing: '.12em', textTransform: 'uppercase', fontWeight: 600 }}>{s}</div>
            {NAV.filter(n => n.section === s).map(n => {
              const ia = isActive(n);
              return (
                <button key={n.id} onClick={() => onNav(n.id)} style={{
                  width: '100%', display: 'flex', alignItems: 'center', gap: 10,
                  padding: '9px 10px', borderRadius: 8, border: 'none',
                  background: ia ? 'rgba(255,255,255,.1)' : 'transparent',
                  color: ia ? '#fff' : 'rgba(255,255,255,.65)',
                  cursor: 'pointer', textAlign: 'left',
                  fontSize: 13, fontWeight: ia ? 500 : 400, marginBottom: 1,
                  position: 'relative',
                }}>
                  {ia && <div style={{ position: 'absolute', left: -8, top: 8, bottom: 8, width: 3, background: '#C9922B', borderRadius: 99 }}/>}
                  <Icon name={n.icon} size={16}/>
                  {n.label}
                </button>
              );
            })}
          </div>
        ))}
      </div>

      {/* Operator footer */}
      <div style={{ padding: '10px 10px 14px', borderTop: '1px solid rgba(255,255,255,.08)' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '8px', borderRadius: 8, background: 'rgba(255,255,255,.05)' }}>
          <div style={{ width: 30, height: 30, borderRadius: 99, background: '#C9922B', display: 'grid', placeItems: 'center', fontSize: 11.5, fontWeight: 700, flexShrink: 0 }}>AD</div>
          <div style={{ flex: 1, minWidth: 0 }}>
            <div style={{ fontSize: 12.5, fontWeight: 500 }}>Adeel</div>
            <div style={{ fontSize: 10.5, color: 'rgba(255,255,255,.45)' }}>Cashier · E1042</div>
          </div>
          <Icon name="settings" size={13} color="rgba(255,255,255,.35)"/>
        </div>
      </div>
    </div>
  );
};

// ── Top status bar ────────────────────────────────────────────────────────────
const StatusBar = ({ onLock }) => {
  const [time, setTime] = React.useState(new Date());
  React.useEffect(() => { const t = setInterval(() => setTime(new Date()), 1000); return () => clearInterval(t); }, []);

  return (
    <div style={{
      height: STATUSBAR_H, background: '#fff', borderBottom: '1px solid var(--line)',
      display: 'flex', alignItems: 'center', padding: '0 18px', gap: 14, flexShrink: 0,
    }}>
      {/* Company */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        <div style={{ width: 28, height: 28, borderRadius: 7, background: 'var(--navy-800)', color: '#fff', display: 'grid', placeItems: 'center', fontSize: 11, fontWeight: 700 }}>RT</div>
        <div style={{ lineHeight: 1.15 }}>
          <div style={{ fontSize: 13, fontWeight: 700, letterSpacing: '-.005em' }}>R Technologies POS</div>
          <div style={{ fontSize: 10, color: 'var(--ink-400)', letterSpacing: '.04em' }}>Enterprise Retail Co.</div>
        </div>
      </div>

      <div style={{ width: 1, height: 24, background: 'var(--line)' }}/>

      {/* Location */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '5px 10px', background: 'var(--surface-2)', borderRadius: 7, cursor: 'pointer', border: '1px solid var(--line)' }}>
        <Icon name="location" size={13} color="var(--ink-500)"/>
        <span style={{ fontSize: 12.5, fontWeight: 500, color: 'var(--ink-800)' }}>Main Branch</span>
        <Icon name="chevron" size={11} color="var(--ink-400)"/>
      </div>

      {/* Terminal */}
      <div style={{ lineHeight: 1.15 }}>
        <div style={{ fontSize: 9.5, color: 'var(--ink-400)', textTransform: 'uppercase', letterSpacing: '.06em' }}>Terminal</div>
        <div className="num" style={{ fontSize: 13, fontWeight: 600 }}>POS-01</div>
      </div>

      <div style={{ width: 1, height: 24, background: 'var(--line)' }}/>

      {/* Operator */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        <div style={{ width: 28, height: 28, borderRadius: 99, background: '#C9922B', color: '#fff', display: 'grid', placeItems: 'center', fontSize: 11.5, fontWeight: 700 }}>AD</div>
        <div style={{ lineHeight: 1.15 }}>
          <div style={{ fontSize: 13, fontWeight: 500 }}>Adeel</div>
          <div style={{ fontSize: 10.5, color: 'var(--ink-400)' }}>Cashier · E1042</div>
        </div>
      </div>

      {/* Shift pill */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 7, padding: '5px 10px', background: 'var(--green-50)', borderRadius: 99 }}>
        <Dot tone="success" size={6}/>
        <span className="num" style={{ fontSize: 11.5, color: 'var(--green-700)', fontWeight: 500 }}>Shift S-4218 open · 5h 42m</span>
      </div>

      <div style={{ flex: 1 }}/>

      {/* Business date + time */}
      <div style={{ lineHeight: 1.15, textAlign: 'right' }}>
        <div style={{ fontSize: 9.5, color: 'var(--ink-400)', textTransform: 'uppercase', letterSpacing: '.06em' }}>Business date</div>
        <div className="num" style={{ fontSize: 13, fontWeight: 600 }}>
          Tue 19 May 2026 · {time.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}
        </div>
      </div>

      <div style={{ width: 1, height: 24, background: 'var(--line)' }}/>

      {/* Sync */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '5px 10px', background: 'var(--green-50)', borderRadius: 7 }}>
        <Icon name="sync" size={13} color="var(--green-700)"/>
        <span style={{ fontSize: 11.5, color: 'var(--green-700)', fontWeight: 500 }}>Synced</span>
      </div>

      {/* Lock */}
      <button onClick={onLock} title="Lock screen" style={{
        width: 34, height: 34, borderRadius: 8, border: '1px solid var(--line)', background: '#fff',
        cursor: 'pointer', display: 'grid', placeItems: 'center', color: 'var(--ink-600)',
      }}>
        <Icon name="lock" size={14}/>
      </button>
    </div>
  );
};

// ── Sub-tabs (reused across sections) ────────────────────────────────────────
const SubTabs = ({ items, active, onChange }) => (
  <div style={{ display: 'flex', gap: 2, padding: '0 24px', background: '#fff', borderBottom: '1px solid var(--line)', flexShrink: 0 }}>
    {items.map(it => (
      <button key={it.id} onClick={() => onChange(it.id)} style={{
        padding: '12px 14px', border: 'none', background: 'transparent', cursor: 'pointer',
        fontSize: 13, fontWeight: 500, fontFamily: 'inherit',
        color: active === it.id ? 'var(--navy-800)' : 'var(--ink-500)',
        borderBottom: '2px solid ' + (active === it.id ? 'var(--navy-800)' : 'transparent'),
        marginBottom: -1,
      }}>{it.label}</button>
    ))}
  </div>
);

// ── Root App ──────────────────────────────────────────────────────────────────
const App = () => {
  // Auth states: account-login | pin-login | shift-open | (main app routes)
  const [route, setRoute] = React.useState("account-login");
  const [loggedAccount, setLoggedAccount] = React.useState("");

  const [paymentCtx, setPaymentCtx] = React.useState({ total: 3885, cart: EXAMPLE_CART });
  const [mgrModal,  setMgrModal]  = React.useState(null);
  const [voidModal, setVoidModal] = React.useState(null);
  const [reportTab, setReportTab] = React.useState("z");

  const handleNav = (id) => {
    const map = {
      checkout: "checkout", orders: "checkout", shift: "shift-close",
      cash: "cash", reports: "reports", items: "catalog", people: "people", admin: "admin",
    };
    setRoute(map[id] || id);
  };

  const renderScreen = () => {
    switch (route) {
      // ── Auth ──────────────────────────────────────────────────────────────
      case "account-login":
        return <AccountLoginScreen onLogin={(acc) => { setLoggedAccount(acc || "Adeel"); setRoute("pin-login"); }}/>;
      case "pin-login":
        return <PinLoginScreen employeeName={loggedAccount || "Adeel"} onPinLogin={() => setRoute("shift-open")} onLogout={() => setRoute("account-login")}/>;
      case "shift-open":
        return <ShiftOpenScreen onOpen={() => setRoute("checkout")} onCancel={() => setRoute("pin-login")}/>;

      // ── Checkout ──────────────────────────────────────────────────────────
      case "checkout":
        return <CheckoutScreen
          goPayment={(total, cart) => { setPaymentCtx({ total, cart }); setRoute("payment"); }}
          openManager={(action) => setMgrModal(action || "Manager override")}
          openVoid={(line) => setVoidModal(line)}
        />;
      case "payment":
        return <PaymentScreen total={paymentCtx.total} cart={paymentCtx.cart}
          onComplete={() => setRoute("receipt")} onBack={() => setRoute("checkout")}/>;
      case "receipt":
        return <ReceiptScreen onNew={() => setRoute("checkout")}/>;

      // ── Cash ──────────────────────────────────────────────────────────────
      case "cash":
        return (
          <div style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
            <SubTabs active="drawer" onChange={(id) => setRoute(id === "drawer" ? "cash" : id)} items={[
              { id: "drawer", label: "Cash Drawer" },
              { id: "drawer-to-vault", label: "Drawer → Vault" },
              { id: "vault-to-bank",   label: "Vault → Bank" },
              { id: "ledger", label: "Account ledger" },
            ]}/>
            <div style={{ flex: 1, overflow: 'hidden' }}><CashDrawerScreen onTransfer={() => setRoute("drawer-to-vault")}/></div>
          </div>
        );
      case "drawer-to-vault":
        return (
          <div style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
            <SubTabs active="drawer-to-vault" onChange={(id) => setRoute(id === "drawer" ? "cash" : id)} items={[
              { id: "drawer", label: "Cash Drawer" },
              { id: "drawer-to-vault", label: "Drawer → Vault" },
              { id: "vault-to-bank",   label: "Vault → Bank" },
              { id: "ledger", label: "Account ledger" },
            ]}/>
            <div style={{ flex: 1, overflow: 'hidden' }}><DrawerToVaultScreen onBack={() => setRoute("cash")} onConfirm={() => setMgrModal("Drawer → Vault transfer")}/></div>
          </div>
        );
      case "vault-to-bank":
        return (
          <div style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
            <SubTabs active="vault-to-bank" onChange={(id) => setRoute(id === "drawer" ? "cash" : id)} items={[
              { id: "drawer", label: "Cash Drawer" },
              { id: "drawer-to-vault", label: "Drawer → Vault" },
              { id: "vault-to-bank",   label: "Vault → Bank" },
              { id: "ledger", label: "Account ledger" },
            ]}/>
            <div style={{ flex: 1, overflow: 'hidden' }}><VaultToBankScreen onBack={() => setRoute("cash")} onConfirm={() => setMgrModal("Bank deposit")}/></div>
          </div>
        );

      // ── Shift close ───────────────────────────────────────────────────────
      case "shift-close":
        return <ShiftCloseScreen onClose={() => setRoute("pin-login")} onBack={() => setRoute("checkout")}/>;

      // ── Reports ───────────────────────────────────────────────────────────
      case "reports":
        return (
          <div style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
            <SubTabs active={reportTab} onChange={setReportTab} items={[
              { id: "z", label: "Z-Report" },
              { id: "daily", label: "Daily sales" },
              { id: "employee", label: "Employee-wise" },
            ]}/>
            <div style={{ flex: 1, overflow: 'hidden' }}>
              {reportTab === "z"        && <ZReportScreen/>}
              {reportTab === "daily"    && <DailySalesReport/>}
              {reportTab === "employee" && <EmployeeReport/>}
            </div>
          </div>
        );

      // ── Management ────────────────────────────────────────────────────────
      case "catalog": return <ItemManagementScreen/>;
      case "people":  return <EmployeeTerminalScreen/>;
      case "admin":   return <AdminDashboard go={setRoute}/>;

      default: return null;
    }
  };

  const fullscreen = ["account-login", "pin-login"].includes(route);

  return (
    <>
      {fullscreen ? renderScreen() : (
        <div style={{ display: 'flex', width: '100%', height: '100%', overflow: 'hidden' }}>
          <Sidebar active={route} onNav={handleNav}/>
          <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
            <StatusBar onLock={() => setRoute("pin-login")}/>
            <HardwareStrip/>
            <div style={{ flex: 1, overflow: 'hidden', position: 'relative' }}>
              {renderScreen()}
            </div>
          </div>
        </div>
      )}

      {/* Global modals */}
      <ManagerApprovalModal
        open={!!mgrModal} action={mgrModal}
        onClose={() => setMgrModal(null)} onApprove={() => setMgrModal(null)} onReject={() => setMgrModal(null)}
      />
      <VoidItemModal
        open={!!voidModal} line={voidModal}
        onClose={() => setVoidModal(null)} onConfirm={() => setVoidModal(null)}
        onRequestManager={() => { setVoidModal(null); setMgrModal("Void item · manager approval required"); }}
      />

      {/* Reviewer navigator */}
      <PrototypeNav route={route} setRoute={setRoute} setLoggedAccount={setLoggedAccount}/>
    </>
  );
};

// ── Prototype navigator (floating) ───────────────────────────────────────────
const PrototypeNav = ({ route, setRoute, setLoggedAccount }) => {
  const [open, setOpen] = React.useState(false);
  const screens = [
    { id: "account-login",   label: "1. Account / password login" },
    { id: "pin-login",       label: "2. PIN login (locked screen)" },
    { id: "shift-open",      label: "3. Shift open" },
    { id: "checkout",        label: "4–7. Checkout · search · grid · cart" },
    { id: "payment",         label: "8. Payment screen" },
    { id: "receipt",         label: "9. Receipt / order complete" },
    { id: "_mgr",            label: "10. Manager approval modal" },
    { id: "_void",           label: "11. Void item flow" },
    { id: "cash",            label: "12. Cash drawer" },
    { id: "drawer-to-vault", label: "13. Drawer → Vault" },
    { id: "vault-to-bank",   label: "14. Vault → Bank" },
    { id: "shift-close",     label: "15. Shift close" },
    { id: "reports",         label: "16–18. Reports (Z · Daily · Employee)" },
    { id: "catalog",         label: "19. Item management" },
    { id: "people",          label: "20. People & devices" },
    { id: "admin",           label: "21. Admin dashboard" },
  ];
  const go = (s) => {
    if (s.id === "_mgr")  { setRoute("checkout"); }
    else if (s.id === "_void") { setRoute("checkout"); }
    else {
      if (["account-login","pin-login"].includes(s.id)) setLoggedAccount?.("Adeel");
      setRoute(s.id);
    }
    setOpen(false);
  };

  return (
    <div style={{ position: 'fixed', bottom: 16, right: 16, zIndex: 9999 }}>
      {open && (
        <div style={{
          background: '#fff', borderRadius: 12, boxShadow: 'var(--shadow-3)',
          width: 320, padding: 6, marginBottom: 8, border: '1px solid var(--line)',
          maxHeight: 520, overflow: 'auto',
        }}>
          <div style={{ padding: '8px 10px 6px', fontSize: 10.5, color: 'var(--ink-400)', textTransform: 'uppercase', letterSpacing: '.06em', fontWeight: 500 }}>Jump to screen</div>
          {screens.map(s => (
            <button key={s.id} onClick={() => go(s)} style={{
              width: '100%', padding: '8px 10px', border: 'none', borderRadius: 7, cursor: 'pointer', textAlign: 'left',
              background: route === s.id ? 'var(--blue-50)' : 'transparent',
              color: route === s.id ? 'var(--navy-800)' : 'var(--ink-700)',
              fontSize: 12.5, fontFamily: 'inherit', display: 'flex', alignItems: 'center', gap: 8, fontWeight: route === s.id ? 500 : 400,
            }}>
              {route === s.id ? <Dot tone="blue"/> : <span style={{ width: 8 }}/>}
              {s.label}
            </button>
          ))}
        </div>
      )}
      <button onClick={() => setOpen(o => !o)} style={{
        height: 40, padding: '0 16px', background: 'var(--navy-800)', color: '#fff',
        border: 'none', borderRadius: 99, cursor: 'pointer', boxShadow: 'var(--shadow-2)',
        display: 'inline-flex', alignItems: 'center', gap: 8, fontSize: 13, fontWeight: 500, fontFamily: 'inherit',
      }}>
        <Icon name="grid" size={14}/> {open ? "Close" : "Screens"}
      </button>
    </div>
  );
};

ReactDOM.createRoot(document.getElementById('app')).render(<App/>);
