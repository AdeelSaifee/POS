// Auth screens v2 — matches imogyn Technologies branding + OrderPin reference layout
// Account/password login, PIN login with cash-register keypad, lock/logout flow

// ── Background (POS environment visual) ──────────────────────────────────────
const LoginBG = () =>
<div style={{ position: 'absolute', inset: 0, overflow: 'hidden', zIndex: 0 }}>
    {/* Deep navy base */}
    <div style={{ position: 'absolute', inset: 0, background: 'linear-gradient(135deg, #07152B 0%, #0d2247 55%, #071830 100%)' }} />
    {/* Subtle dot grid */}
    <svg style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', opacity: 0.07 }}>
      <defs>
        <pattern id="dotgrid" x="0" y="0" width="28" height="28" patternUnits="userSpaceOnUse">
          <circle cx="1" cy="1" r="1" fill="#fff" />
        </pattern>
      </defs>
      <rect width="100%" height="100%" fill="url(#dotgrid)" />
    </svg>
    {/* Abstract POS monitor silhouette — top right */}
    <svg viewBox="0 0 500 360" style={{ position: 'absolute', right: -60, top: -40, width: 560, opacity: 0.06 }}>
      <rect x="60" y="20" width="380" height="250" rx="14" fill="none" stroke="#fff" strokeWidth="2" />
      <rect x="60" y="20" width="380" height="250" rx="14" fill="#fff" opacity="0.04" />
      <rect x="200" y="270" width="100" height="30" rx="4" fill="#fff" opacity="0.5" />
      <rect x="130" y="300" width="240" height="8" rx="4" fill="#fff" opacity="0.5" />
      <rect x="80" y="40" width="340" height="6" rx="2" fill="#fff" opacity="0.3" />
      <rect x="80" y="60" width="180" height="100" rx="4" fill="none" stroke="#fff" strokeWidth="1" />
      <rect x="80" y="60" width="180" height="100" rx="4" fill="#fff" opacity="0.03" />
      <rect x="270" y="60" width="150" height="46" rx="4" fill="none" stroke="#fff" strokeWidth="1" />
      <rect x="270" y="60" width="150" height="46" rx="4" fill="#fff" opacity="0.03" />
      <rect x="270" y="116" width="72" height="44" rx="4" fill="none" stroke="#fff" strokeWidth="1" />
      <rect x="350" y="116" width="70" height="44" rx="4" fill="none" stroke="#fff" strokeWidth="1" />
      {[0, 1, 2, 3, 4].map((i) => <rect key={i} x={80} y={175 + i * 18} width={120} height={8} rx="2" fill="#fff" opacity="0.15" />)}
    </svg>
    {/* Glow bottom-left */}
    <div style={{ position: 'absolute', bottom: -80, left: -80, width: 360, height: 360, borderRadius: '50%', background: 'radial-gradient(circle, rgba(201,146,43,.18) 0%, transparent 70%)' }} />
    {/* Glow top-center */}
    <div style={{ position: 'absolute', top: -60, left: '40%', width: 320, height: 320, borderRadius: '50%', background: 'radial-gradient(circle, rgba(30,91,214,.14) 0%, transparent 70%)' }} />
  </div>;


// ── Shared card shell ─────────────────────────────────────────────────────────
const AuthCard = ({ leftContent, rightContent }) =>
<div style={{
  position: 'absolute', inset: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 10
}}>
    <div style={{
    display: 'flex', width: 900, maxWidth: '96vw', maxHeight: '95vh',
    borderRadius: 16, overflow: 'hidden',
    boxShadow: '0 40px 80px -20px rgba(0,0,0,.7), 0 12px 32px rgba(0,0,0,.4)'
  }}>
      {/* Left dark brand panel */}
      <div style={{
      width: 340, flexShrink: 0,
      background: 'linear-gradient(160deg, #0A1F44 0%, #0F2C5C 60%, #071A38 100%)',
      display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'space-between',
      padding: '44px 32px 28px',

      position: 'relative', overflow: 'hidden', borderRight: "1px rgba(255, 255, 255, 0.06)", borderTopStyle: "none", borderBottomStyle: "none", borderLeftStyle: "none", fontFamily: "\"IBM Plex Sans\""
    }}>
        {/* Panel inner glow */}
        <div style={{ position: 'absolute', top: -60, left: -60, width: 220, height: 220, borderRadius: '50%', background: 'radial-gradient(circle, rgba(201,146,43,.12) 0%, transparent 70%)' }} />
        <div style={{ position: 'absolute', bottom: -40, right: -40, width: 180, height: 180, borderRadius: '50%', background: 'radial-gradient(circle, rgba(30,91,214,.1) 0%, transparent 70%)' }} />

        {/* Logo + tagline */}
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 20, position: 'relative', zIndex: 1 }}>
          <img src=(window.__resources && window.__resources.logo) || "assets/logo.png" alt="imogyn Technologies" style={{ width: 200, filter: 'brightness(1.1)' }} />
          <div style={{
          width: '100%', height: 1,
          background: 'linear-gradient(90deg, transparent, rgba(255,255,255,.15), transparent)'
        }} />
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontSize: 13.5, color: 'rgba(255,255,255,.75)', fontWeight: 500, letterSpacing: '.01em' }}>Enterprise POS Platform</div>
            <div style={{ fontSize: 11.5, color: 'rgba(255,255,255,.4)', marginTop: 5, letterSpacing: '.04em', textTransform: 'uppercase' }}>Multi-location · Multi-tenant</div>
          </div>
        </div>

        {/* Hardware status dots */}
        <div style={{
        display: 'flex', flexDirection: 'column', gap: 10, position: 'relative', zIndex: 1,
        padding: '14px 18px', background: 'rgba(255,255,255,.04)', borderRadius: 10,
        border: '1px solid rgba(255,255,255,.06)', width: '100%'
      }}>
          <div style={{ fontSize: 10, color: 'rgba(255,255,255,.35)', textTransform: 'uppercase', letterSpacing: '.08em', fontWeight: 500, marginBottom: 2 }}>Terminal status</div>
          {[
        { icon: 'print', label: 'Printer', ok: true },
        { icon: 'drawer', label: 'Cash drawer', ok: true },
        { icon: 'scan', label: 'Scanner', ok: true },
        { icon: 'terminal', label: 'Payment terminal', ok: true },
        { icon: 'sync', label: 'Network', ok: true }].
        map((s) =>
        <div key={s.label} style={{ display: 'flex', alignItems: 'center', gap: 10, fontSize: 12 }}>
              <Dot tone={s.ok ? 'success' : 'danger'} />
              <Icon name={s.icon} size={14} color="rgba(255,255,255,.45)" />
              <span style={{ flex: 1, color: 'rgba(255,255,255,.65)' }}>{s.label}</span>
              <span style={{ color: s.ok ? '#4ade80' : '#f87171', fontSize: 11 }}>{s.ok ? 'Ready' : 'Error'}</span>
            </div>
        )}
        </div>

        {/* Footer */}
        <div style={{ fontSize: 11.5, color: 'rgba(255,255,255,.3)', display: 'flex', gap: 14, position: 'relative', zIndex: 1 }}>
          <span style={{ cursor: 'pointer' }}>User Agreement</span>
          <span>|</span>
          <span style={{ cursor: 'pointer' }}>Privacy Policy</span>
        </div>
      </div>

      {/* Right white form panel */}
      <div style={{ flex: 1, background: '#fff', display: 'flex', flexDirection: 'column' }}>
        {rightContent}
      </div>
    </div>
  </div>;


// ── Account / Password login ──────────────────────────────────────────────────
const AccountLoginScreen = ({ onLogin }) => {
  const [account, setAccount] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [showPw, setShowPw] = React.useState(false);
  const [error, setError] = React.useState("");
  const [loading, setLoading] = React.useState(false);

  const handleLogin = () => {
    if (!account || !password) {setError("Please enter your account and password.");return;}
    setLoading(true);setError("");
    setTimeout(() => {setLoading(false);onLogin?.(account);}, 800);
  };

  return (
    <div style={{ position: 'absolute', inset: 0 }}>
      <LoginBG />
      <AuthCard
        rightContent={
        <div style={{ flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center', padding: '52px 52px' }}>
            <h1 style={{ margin: '0 0 6px', fontSize: 28, fontWeight: 700, letterSpacing: '-.01em', textAlign: 'center', color: 'var(--ink-900)' }}>Login</h1>
            <p style={{ margin: '0 0 32px', fontSize: 13, color: 'var(--ink-400)', textAlign: 'center' }}>Sign in to your POS account</p>

            <Field label="Account" style={{ marginBottom: 18 }}>
              <div style={{
              display: 'flex', alignItems: 'center', gap: 10,
              border: '1.5px solid ' + (error && !account ? 'var(--red-600)' : 'var(--line-hard)'),
              borderRadius: 8, padding: '0 14px', height: 52,
              background: '#fff', transition: 'border-color .15s'
            }}>
                <Icon name="user" size={18} color="var(--ink-400)" />
                <input
                value={account} onChange={(e) => {setAccount(e.target.value);setError("");}}
                onKeyDown={(e) => e.key === 'Enter' && handleLogin()}
                placeholder="Enter your account"
                style={{ flex: 1, border: 'none', outline: 'none', fontSize: 15, fontFamily: 'inherit', color: 'var(--ink-900)' }} />
              
              </div>
            </Field>

            <Field label="Password" style={{ marginBottom: 10 }}>
              <div style={{
              display: 'flex', alignItems: 'center', gap: 10,
              border: '1.5px solid ' + (error && !password ? 'var(--red-600)' : 'var(--line-hard)'),
              borderRadius: 8, padding: '0 14px', height: 52,
              background: '#fff', transition: 'border-color .15s'
            }}>
                <Icon name="lock" size={18} color="var(--ink-400)" />
                <input
                type={showPw ? "text" : "password"}
                value={password} onChange={(e) => {setPassword(e.target.value);setError("");}}
                onKeyDown={(e) => e.key === 'Enter' && handleLogin()}
                placeholder="Enter your password"
                style={{ flex: 1, border: 'none', outline: 'none', fontSize: 15, fontFamily: 'inherit', color: 'var(--ink-900)' }} />
              
                <button onClick={() => setShowPw((s) => !s)} style={{ border: 'none', background: 'transparent', cursor: 'pointer', padding: 4, color: 'var(--ink-400)' }}>
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
                    {showPw ? <><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19M1 1l22 22" /></> :
                  <><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" /><circle cx="12" cy="12" r="3" /></>}
                  </svg>
                </button>
              </div>
            </Field>

            {error &&
          <div style={{ marginBottom: 14, padding: '10px 14px', background: 'var(--red-50)', border: '1px solid var(--red-100)', borderRadius: 8, fontSize: 12.5, color: 'var(--red-700)', display: 'flex', gap: 8, alignItems: 'center' }}>
                <Icon name="warning" size={15} color="var(--red-700)" />{error}
              </div>
          }

            <button
            onClick={handleLogin} disabled={loading}
            style={{
              width: '100%', height: 52, borderRadius: 8, marginBottom: 18, marginTop: 6,
              border: 'none', cursor: loading ? 'wait' : 'pointer',
              background: loading ? 'var(--ink-300)' : 'var(--navy-800)',
              color: '#fff', fontSize: 16, fontWeight: 600, fontFamily: 'inherit',
              letterSpacing: '.01em', transition: 'background .15s',
              display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 10
            }}>
              {loading ?
            <><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2.5" strokeLinecap="round"><path d="M21 12a9 9 0 1 1-6.22-8.56" /></svg> Signing in…</> :
            "Login"}
            </button>

            <div style={{ textAlign: 'center' }}>
              <button style={{ background: 'none', border: 'none', color: 'var(--blue-600)', fontSize: 13, cursor: 'pointer', fontFamily: 'inherit', textDecoration: 'underline', textUnderlineOffset: 3 }}>
                How to get an account to login?
              </button>
            </div>

            <div style={{ marginTop: 'auto', paddingTop: 32, borderTop: '1px solid var(--line)', marginTop: 40 }}>
              <div style={{ display: 'flex', gap: 10, alignItems: 'center', padding: '10px 14px', background: 'var(--surface-2)', borderRadius: 8 }}>
                <Icon name="terminal" size={16} color="var(--ink-400)" />
                <div style={{ fontSize: 12 }}>
                  <span style={{ color: 'var(--ink-500)' }}>Terminal </span>
                  <span className="num" style={{ fontWeight: 500, color: 'var(--ink-800)' }}>POS-01 · Main Branch · R Technologies POS</span>
                </div>
              </div>
            </div>
          </div>
        } />
      
    </div>);

};

// ── PIN login ─────────────────────────────────────────────────────────────────
const PinLoginScreen = ({ employeeName = "Adeel", onPinLogin, onLogout }) => {
  const [pin, setPin] = React.useState("");
  const [shake, setShake] = React.useState(false);
  const [showAttendance, setShowAttendance] = React.useState(false);
  const PIN_LEN = 4;

  const handleKey = (k) => {
    if (k === "C") {setPin("");return;}
    if (k === "⌫") {setPin((p) => p.slice(0, -1));return;}
    if (pin.length >= PIN_LEN) return;
    const next = pin + k;
    setPin(next);
    if (next.length === PIN_LEN) {
      // Simulate verify — any 4-digit accepted for prototype
      setTimeout(() => {
        if (next === "0000") {
          setShake(true);setPin("");setTimeout(() => setShake(false), 500);
        } else {
          onPinLogin?.();
        }
      }, 250);
    }
  };

  // Cash register layout: 7 8 9 / 4 5 6 / 1 2 3 / C 0 ⌫
  const KEYS = ["7", "8", "9", "4", "5", "6", "1", "2", "3", "C", "0", "⌫"];

  return (
    <div style={{ position: 'absolute', inset: 0 }}>
      <LoginBG />
      <AuthCard
        rightContent={
        <div style={{ flex: 1, display: 'flex', flexDirection: 'column', padding: '24px 44px 32px', minHeight: 0 }}>
            {/* Attendance button top right */}
            <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 16 }}>
              <button
              onClick={() => setShowAttendance(true)}
              style={{
                display: 'inline-flex', alignItems: 'center', gap: 7, height: 36, padding: '0 14px',
                border: '1.5px solid var(--line-hard)', borderRadius: 8, background: '#fff',
                fontSize: 13, fontWeight: 500, color: 'var(--ink-700)', cursor: 'pointer', fontFamily: 'inherit'
              }}>
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
                  <circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" />
                </svg>
                Attendance
              </button>
            </div>

            {/* Employee avatar + name */}
            <div style={{ textAlign: 'center', marginBottom: 22 }}>
              <div style={{ width: 64, height: 64, borderRadius: 99, background: 'var(--navy-800)', color: '#fff', display: 'inline-flex', alignItems: 'center', justifyContent: 'center', fontSize: 22, fontWeight: 700, marginBottom: 12, boxShadow: '0 4px 14px rgba(15,28,92,.2)' }}>
                {employeeName.slice(0, 2).toUpperCase()}
              </div>
              <h2 style={{ margin: 0, fontSize: 24, fontWeight: 700, letterSpacing: '-.01em', color: 'var(--ink-900)' }}>{employeeName}</h2>
              <div style={{ fontSize: 12.5, color: 'var(--ink-500)', marginTop: 4 }}>Cashier · Main Branch</div>
            </div>

            {/* PIN dots */}
            <div style={{ display: 'flex', justifyContent: 'center', gap: 14, marginBottom: 22 }}>
              {Array.from({ length: PIN_LEN }).map((_, i) =>
            <div key={i} style={{
              width: 52, height: 60, borderRadius: 10,
              border: '1.5px solid ' + (shake ? 'var(--red-500)' : i < pin.length ? 'var(--navy-800)' : 'var(--line-hard)'),
              background: i < pin.length ? 'var(--navy-800)' : '#fff',
              display: 'grid', placeItems: 'center',
              fontSize: 32, fontWeight: 700,
              color: i < pin.length ? '#fff' : 'var(--ink-200)',
              transition: 'all .12s',
              boxShadow: i < pin.length ? '0 4px 12px rgba(15,28,92,.18)' : 'none'
            }}>{i < pin.length ? "●" : ""}</div>
            )}
            </div>

            {/* POS-style PIN pad: 7-8-9 / 4-5-6 / 1-2-3 / C-0-⌫ */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 10, flex: 1 }}>
              {KEYS.map((k) => {
              const isAction = k === "C" || k === "⌫";
              return (
                <button key={k} onClick={() => handleKey(k)} style={{
                  borderRadius: 10,
                  border: '1.5px solid ' + (isAction ? 'var(--line)' : 'var(--line-hard)'),
                  background: isAction ? 'var(--surface-2)' : '#fff',
                  color: k === "C" ? 'var(--red-600)' : isAction ? 'var(--ink-600)' : 'var(--ink-900)',
                  fontSize: k === "⌫" ? 18 : 26, fontWeight: 500, fontFamily: k === "⌫" ? 'inherit' : 'var(--mono)',
                  cursor: 'pointer',
                  transition: 'background .08s, transform .04s',
                  boxShadow: 'var(--shadow-1)',
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                  minHeight: 56
                }}
                onMouseDown={(e) => e.currentTarget.style.transform = 'scale(.95)'}
                onMouseUp={(e) => e.currentTarget.style.transform = 'scale(1)'}
                onMouseLeave={(e) => e.currentTarget.style.transform = 'scale(1)'}>
                  
                    {k === "⌫" ?
                  <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 4H8L2 12l6 8h13a2 2 0 0 0 2-2V6a2 2 0 0 0-2-2z" /><line x1="18" y1="9" x2="13" y2="14" /><line x1="13" y1="9" x2="18" y2="14" /></svg> :
                  k}
                  </button>);

            })}
            </div>

            {/* Bottom links */}
            <div style={{ marginTop: 18, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8 }}>
              <button onClick={onLogout} style={{ background: 'none', border: 'none', color: 'var(--blue-600)', fontSize: 13.5, cursor: 'pointer', fontFamily: 'inherit', fontWeight: 500, textDecoration: 'underline', textUnderlineOffset: 3 }}>
                Log out
              </button>
              <button style={{ background: 'none', border: 'none', color: 'var(--ink-400)', fontSize: 12.5, cursor: 'pointer', fontFamily: 'inherit' }}>
                How to get a PIN code to login?
              </button>
            </div>
          </div>
        } />
      

      {/* Attendance mini-modal */}
      <Modal open={showAttendance} onClose={() => setShowAttendance(false)} width={400}>
        <ModalHeader title="Mark attendance" subtitle={`${employeeName} · ${new Date().toLocaleDateString('en-GB', { dateStyle: 'full' })}`} onClose={() => setShowAttendance(false)} />
        <div style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 12 }}>
          <Btn variant="success" size="lg" full icon="check" onClick={() => setShowAttendance(false)}>Clock In</Btn>
          <Btn variant="default" size="lg" full icon="close" onClick={() => setShowAttendance(false)}>Clock Out</Btn>
        </div>
      </Modal>
    </div>);

};

// ── Shift open (unchanged but kept here for clarity) ──────────────────────────
const ShiftOpenScreen = ({ onOpen, onCancel }) => {
  const [opening, setOpening] = React.useState("15000");
  const [notes, setNotes] = React.useState("");
  const denominations = [5000, 1000, 500, 100, 50, 20, 10, 5];
  const [denCounts, setDenCounts] = React.useState({});
  const totalFromDen = Object.entries(denCounts).reduce((s, [d, c]) => s + Number(d) * Number(c || 0), 0);

  return (
    <div style={{ padding: 32, display: 'flex', justifyContent: 'center', height: '100%', overflow: 'auto' }}>
      <div style={{ width: '100%', maxWidth: 1100 }}>
        <div style={{ marginBottom: 20 }}>
          <div style={{ fontSize: 11.5, color: 'var(--ink-400)', letterSpacing: '.08em', textTransform: 'uppercase', fontWeight: 500 }}>Shift</div>
          <h1 style={{ margin: '4px 0 0', fontSize: 28, fontWeight: 600, letterSpacing: '-.02em' }}>Open shift on POS-01</h1>
          <p style={{ margin: '4px 0 0', color: 'var(--ink-500)', fontSize: 14 }}>Declare opening drawer cash for business date <span className="num">19 May 2026</span>.</p>
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 360px', gap: 24, alignItems: 'start' }}>
          <Card padding={24}>
            <SectionTitle>Drawer count</SectionTitle>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 10, marginBottom: 18 }}>
              {denominations.map((d) =>
              <div key={d} style={{ border: '1px solid var(--line)', borderRadius: 10, padding: 12 }}>
                  <div className="num" style={{ fontSize: 13, color: 'var(--ink-500)', marginBottom: 8 }}>PKR {d.toLocaleString()}</div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <button onClick={() => setDenCounts((c) => ({ ...c, [d]: Math.max(0, (Number(c[d]) || 0) - 1) }))} style={{ width: 28, height: 28, borderRadius: 6, border: '1px solid var(--line)', background: '#fff', cursor: 'pointer' }}><Icon name="minus" size={14} /></button>
                    <input className="num" value={denCounts[d] || ""} onChange={(e) => setDenCounts((c) => ({ ...c, [d]: e.target.value.replace(/\D/g, '') }))} placeholder="0" style={{ flex: 1, height: 32, border: '1px solid var(--line-hard)', borderRadius: 6, padding: '0 8px', fontSize: 15, fontFamily: 'var(--mono)', textAlign: 'center' }} />
                    <button onClick={() => setDenCounts((c) => ({ ...c, [d]: (Number(c[d]) || 0) + 1 }))} style={{ width: 28, height: 28, borderRadius: 6, border: '1px solid var(--line)', background: '#fff', cursor: 'pointer' }}><Icon name="plus" size={14} /></button>
                  </div>
                </div>
              )}
            </div>
            <Field label="Or enter total opening cash" style={{ marginBottom: 16 }}><Input full value={opening} onChange={(e) => setOpening(e.target.value)} /></Field>
            <Field label="Notes (optional)"><textarea value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="e.g. Float carried from yesterday close" style={{ width: '100%', minHeight: 70, border: '1px solid var(--line-hard)', borderRadius: 8, padding: 12, fontSize: 13, fontFamily: 'inherit', resize: 'vertical' }} /></Field>
          </Card>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
            <Card padding={20}>
              <div style={{ fontSize: 11.5, color: 'var(--ink-400)', textTransform: 'uppercase', letterSpacing: '.05em', fontWeight: 500 }}>Counted opening</div>
              <div className="num" style={{ fontSize: 40, fontWeight: 600, letterSpacing: '-.02em', marginTop: 6 }}>{fmtT(totalFromDen || opening)}</div>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 14, padding: '10px 0 0', borderTop: '1px dashed var(--line)', fontSize: 12.5 }}>
                <span style={{ color: 'var(--ink-500)' }}>Expected</span><span className="num">PKR 15,000</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 6, fontSize: 12.5 }}>
                <span style={{ color: 'var(--ink-500)' }}>Variance</span><span className="num" style={{ color: 'var(--green-700)' }}>PKR 0</span>
              </div>
            </Card>
            <Card padding={16} style={{ background: 'var(--amber-50)', borderColor: 'var(--amber-100)' }}>
              <div style={{ display: 'flex', gap: 10 }}><Icon name="warning" size={18} color="var(--amber-700)" /><div style={{ fontSize: 12.5, color: 'var(--amber-700)', lineHeight: 1.5 }}>Out-of-shift cash will be locked in the drawer until you sign in with manager PIN.</div></div>
            </Card>
            <Btn variant="success" size="xl" full onClick={onOpen}><Icon name="check" size={18} /> Open shift</Btn>
            <Btn variant="default" size="md" full onClick={onCancel}>Cancel</Btn>
          </div>
        </div>
      </div>
    </div>);

};

// ── Shift close (unchanged) ───────────────────────────────────────────────────
const ShiftCloseScreen = ({ onClose: onCloseShift, onBack }) => {
  const [counted, setCounted] = React.useState("48750");
  const expected = 48885;
  const variance = Number(counted || 0) - expected;
  return (
    <div style={{ padding: 32, height: '100%', overflow: 'auto' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 18 }}>
        <button onClick={onBack} style={{ background: 'transparent', border: 'none', cursor: 'pointer', color: 'var(--ink-500)', display: 'inline-flex', alignItems: 'center', gap: 4 }}><Icon name="chevronL" size={16} /> Back</button>
      </div>
      <div style={{ marginBottom: 20 }}>
        <div style={{ fontSize: 11.5, color: 'var(--ink-400)', letterSpacing: '.08em', textTransform: 'uppercase', fontWeight: 500 }}>Shift S-4218 · Adeel · POS-01</div>
        <h1 style={{ margin: '4px 0 0', fontSize: 28, fontWeight: 600, letterSpacing: '-.02em' }}>Close shift</h1>
        <p style={{ margin: '4px 0 0', color: 'var(--ink-500)', fontSize: 14 }}>Count drawer cash to reconcile expected vs counted.</p>
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24, marginBottom: 24 }}>
        <Card padding={22}>
          <SectionTitle>Drawer reconciliation</SectionTitle>
          <KV k="Opening cash" v="PKR 15,000" mono /><KV k="Cash sales" v="+ PKR 38,200" mono color="var(--green-700)" /><KV k="Card sales" v="PKR 42,150" mono color="var(--ink-400)" /><KV k="Refunds (cash)" v="− PKR 1,200" mono color="var(--red-700)" /><KV k="Payouts" v="− PKR 800" mono color="var(--red-700)" /><KV k="Drawer-to-Vault" v="− PKR 2,315" mono color="var(--ink-500)" /><KV k="Expected drawer cash" v="PKR 48,885" mono strong big divider />
          <Field label="Counted cash" style={{ marginTop: 18 }}><Input full value={counted} onChange={(e) => setCounted(e.target.value.replace(/\D/g, ''))} /></Field>
          <div style={{ marginTop: 16, padding: 14, borderRadius: 10, background: variance === 0 ? 'var(--green-50)' : variance < 0 ? 'var(--red-50)' : 'var(--amber-50)', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <Icon name={variance === 0 ? "check" : "warning"} size={18} color={variance === 0 ? 'var(--green-700)' : variance < 0 ? 'var(--red-700)' : 'var(--amber-700)'} />
              <div style={{ fontSize: 13, color: variance === 0 ? 'var(--green-700)' : variance < 0 ? 'var(--red-700)' : 'var(--amber-700)' }}>{variance === 0 ? "Drawer reconciled" : variance < 0 ? `Short by ${fmtT(Math.abs(variance))}` : `Over by ${fmtT(variance)}`}</div>
            </div>
            <span className="num" style={{ fontSize: 18, fontWeight: 600, color: variance === 0 ? 'var(--green-700)' : variance < 0 ? 'var(--red-700)' : 'var(--amber-700)' }}>{variance >= 0 ? "+" : "−"}PKR {Math.abs(variance).toLocaleString()}</span>
          </div>
        </Card>
        <Card padding={22}>
          <SectionTitle>Shift summary</SectionTitle>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 16 }}>
            <Stat label="Orders" value="184" sub="2 voided" /><Stat label="Gross sales" value="PKR 86,580" /><Stat label="Tax collected" value="PKR 4,210" /><Stat label="Approvals" value="6" sub="3 by Fahad" />
          </div>
          <SectionTitle>Tender mix</SectionTitle>
          {[{ k: "Cash", v: 38200, c: "var(--green-600)" }, { k: "Card", v: 42150, c: "var(--blue-600)" }, { k: "Wallet", v: 4980, c: "var(--amber-600)" }, { k: "Voucher", v: 1250, c: "var(--ink-400)" }].map((t) => {
            const pct = t.v / 86580 * 100;
            return <div key={t.k} style={{ marginBottom: 10 }}><div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12.5, marginBottom: 4 }}><span>{t.k}</span><span className="num">{fmtT(t.v)} · {pct.toFixed(1)}%</span></div><div style={{ height: 6, background: 'var(--surface-2)', borderRadius: 99 }}><div style={{ height: '100%', width: `${pct}%`, background: t.c, borderRadius: 99 }} /></div></div>;
          })}
        </Card>
      </div>
      <div style={{ display: 'flex', gap: 12, justifyContent: 'flex-end' }}>
        <Btn variant="default" icon="print">Print summary</Btn>
        <Btn variant="amber" icon="user">Request manager approval</Btn>
        <Btn variant="primary" icon="check" size="lg" onClick={onCloseShift}>Close shift</Btn>
      </div>
    </div>);

};

Object.assign(window, { AccountLoginScreen, PinLoginScreen, ShiftOpenScreen, ShiftCloseScreen });