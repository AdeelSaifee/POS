// Shared UI primitives + design tokens

const Icon = ({ name, size = 16, color = "currentColor", strokeWidth = 1.75 }) => {
  const paths = {
    cart: <><circle cx="9" cy="20" r="1.5"/><circle cx="18" cy="20" r="1.5"/><path d="M3 4h2l2.5 12h12L22 7H6"/></>,
    cash: <><rect x="2" y="6" width="20" height="12" rx="2"/><circle cx="12" cy="12" r="3"/><path d="M6 9.5v.01M18 14.5v.01"/></>,
    chart: <><path d="M3 3v18h18"/><path d="M7 14l4-4 3 3 5-6"/></>,
    catalog: <><path d="M4 5a2 2 0 0 1 2-2h12v18H6a2 2 0 0 1-2-2V5z"/><path d="M8 7h8M8 11h8M8 15h5"/></>,
    admin: <><circle cx="12" cy="8" r="3"/><path d="M4 21c0-4 4-6 8-6s8 2 8 6"/></>,
    shift: <><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></>,
    search: <><circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></>,
    scan: <><path d="M3 7V5a2 2 0 0 1 2-2h2M17 3h2a2 2 0 0 1 2 2v2M21 17v2a2 2 0 0 1-2 2h-2M7 21H5a2 2 0 0 1-2-2v-2"/><path d="M7 8v8M11 8v8M15 8v8M19 8v8"/></>,
    plus: <><path d="M12 5v14M5 12h14"/></>,
    minus: <><path d="M5 12h14"/></>,
    close: <><path d="M6 6l12 12M18 6 6 18"/></>,
    check: <><path d="m5 12 5 5 9-11"/></>,
    chevron: <><path d="m6 9 6 6 6-6"/></>,
    chevronR: <><path d="m9 6 6 6-6 6"/></>,
    chevronL: <><path d="m15 6-6 6 6 6"/></>,
    print: <><path d="M6 9V3h12v6M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2M6 14h12v8H6z"/></>,
    user: <><circle cx="12" cy="8" r="4"/><path d="M4 21c0-4.4 3.6-8 8-8s8 3.6 8 8"/></>,
    lock: <><rect x="5" y="11" width="14" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 8 0v4"/></>,
    drawer: <><rect x="3" y="4" width="18" height="16" rx="2"/><path d="M3 12h18M9 12v3M15 12v3"/></>,
    vault: <><rect x="3" y="4" width="18" height="16" rx="2"/><circle cx="12" cy="12" r="4"/><path d="M12 8v1M12 15v1M16 12h-1M9 12H8"/></>,
    bank: <><path d="M3 10h18M5 10v8M19 10v8M9 10v8M15 10v8M3 21h18M3 10 12 4l9 6"/></>,
    receipt: <><path d="M6 3h12v18l-3-2-3 2-3-2-3 2V3z"/><path d="M9 8h6M9 12h6M9 16h4"/></>,
    sync: <><path d="M21 12a9 9 0 0 1-15 6.7L3 16"/><path d="M3 12A9 9 0 0 1 18 5.3L21 8"/><path d="M21 3v5h-5M3 21v-5h5"/></>,
    offline: <><path d="M3 3l18 18"/><path d="M8.5 8.5A9 9 0 0 0 3 12M12 16v.01M5.5 12.5a7 7 0 0 1 4-2M18.5 12.5a7 7 0 0 0-4-2"/></>,
    warning: <><path d="M12 3 2 21h20L12 3z"/><path d="M12 10v5M12 18v.01"/></>,
    edit: <><path d="M4 20h4l11-11-4-4L4 16v4z"/></>,
    trash: <><path d="M3 6h18M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M6 6l1 14a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2l1-14"/></>,
    settings: <><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 0 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 0 1 0-4h.1a1.7 1.7 0 0 0 1.5-1.1 1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 0 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 0 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1z"/></>,
    arrowR: <><path d="M5 12h14M13 5l7 7-7 7"/></>,
    arrowL: <><path d="M19 12H5M11 5l-7 7 7 7"/></>,
    download: <><path d="M12 3v14M5 12l7 7 7-7M5 21h14"/></>,
    filter: <><path d="M3 5h18l-7 9v6l-4-2v-4z"/></>,
    location: <><path d="M12 22s-7-7-7-13a7 7 0 0 1 14 0c0 6-7 13-7 13z"/><circle cx="12" cy="9" r="2.5"/></>,
    terminal: <><rect x="2" y="4" width="20" height="14" rx="2"/><path d="M2 18l-1 3h22l-1-3M8 8l3 3-3 3M13 14h4"/></>,
    grid: <><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/></>,
    list: <><path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01"/></>,
    qr: <><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/><path d="M14 14h2v2h-2zM18 14h3v3h-3zM14 18h2v3h-2zM18 21h3"/></>,
    dot: <><circle cx="12" cy="12" r="4"/></>,
    star: <><path d="M12 2 15 9l7 1-5 5 1.5 7-6.5-4-6.5 4L7 15 2 10l7-1z"/></>,
  };
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth={strokeWidth} strokeLinecap="round" strokeLinejoin="round" style={{flexShrink:0, display:'inline-block', verticalAlign:'middle'}}>
      {paths[name] || null}
    </svg>
  );
};

// Button variants
const Btn = ({ variant = "default", size = "md", icon, children, onClick, disabled, full, style = {}, ...rest }) => {
  const base = {
    display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: 8,
    border: '1px solid transparent', cursor: disabled ? 'not-allowed' : 'pointer',
    fontWeight: 500, letterSpacing: '-0.005em', whiteSpace: 'nowrap',
    transition: 'background .12s, border-color .12s, transform .04s',
    userSelect: 'none', textDecoration: 'none', opacity: disabled ? 0.5 : 1,
    width: full ? '100%' : 'auto',
  };
  const sizes = {
    sm: { height: 30, padding: '0 10px', fontSize: 12.5, borderRadius: 6 },
    md: { height: 38, padding: '0 14px', fontSize: 13.5, borderRadius: 8 },
    lg: { height: 48, padding: '0 18px', fontSize: 15, borderRadius: 9 },
    xl: { height: 64, padding: '0 22px', fontSize: 17, borderRadius: 10, fontWeight: 600 },
  };
  const variants = {
    primary:   { background: 'var(--navy-800)', color: '#fff', borderColor: 'var(--navy-800)' },
    blue:      { background: 'var(--blue-600)', color: '#fff', borderColor: 'var(--blue-600)' },
    success:   { background: 'var(--green-600)', color: '#fff', borderColor: 'var(--green-600)' },
    danger:    { background: 'var(--red-600)',  color: '#fff', borderColor: 'var(--red-600)' },
    amber:     { background: 'var(--amber-600)', color: '#fff', borderColor: 'var(--amber-600)' },
    default:   { background: '#fff', color: 'var(--ink-800)', borderColor: 'var(--line-hard)' },
    ghost:     { background: 'transparent', color: 'var(--ink-700)', borderColor: 'transparent' },
    outline:   { background: 'transparent', color: 'var(--navy-800)', borderColor: 'var(--navy-800)' },
    dangerOutline: { background: '#fff', color: 'var(--red-600)', borderColor: 'var(--red-100)' },
    soft:      { background: 'var(--surface-2)', color: 'var(--ink-800)', borderColor: 'transparent' },
  };
  return (
    <button onClick={disabled ? null : onClick} disabled={disabled}
      style={{ ...base, ...sizes[size], ...variants[variant], ...style }} {...rest}>
      {icon && <Icon name={icon} size={size === 'sm' ? 14 : size === 'xl' ? 20 : 16} />}
      {children}
    </button>
  );
};

const Badge = ({ tone = "neutral", children, dot, style = {} }) => {
  const tones = {
    neutral: { bg: 'var(--surface-2)', fg: 'var(--ink-700)', dotc: 'var(--ink-400)' },
    success: { bg: 'var(--green-50)', fg: 'var(--green-700)', dotc: 'var(--green-600)' },
    danger:  { bg: 'var(--red-50)',   fg: 'var(--red-700)',   dotc: 'var(--red-600)' },
    amber:   { bg: 'var(--amber-50)', fg: 'var(--amber-700)', dotc: 'var(--amber-600)' },
    blue:    { bg: 'var(--blue-50)',  fg: 'var(--navy-800)',  dotc: 'var(--blue-600)' },
    navy:    { bg: 'var(--navy-800)', fg: '#fff',             dotc: 'var(--blue-100)' },
    outline: { bg: 'transparent',     fg: 'var(--ink-700)',   dotc: 'var(--ink-400)', border: '1px solid var(--line-hard)' },
  };
  const t = tones[tone];
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', gap: 6,
      padding: '3px 8px', borderRadius: 999, background: t.bg, color: t.fg,
      fontSize: 11.5, fontWeight: 500, letterSpacing: '.01em',
      border: t.border || '1px solid transparent', ...style
    }}>
      {dot && <span style={{ width: 6, height: 6, borderRadius: 99, background: t.dotc }}/>}
      {children}
    </span>
  );
};

const Card = ({ children, style = {}, padding = 16, ...rest }) => (
  <div style={{
    background: '#fff', borderRadius: 12, border: '1px solid var(--line)',
    padding, ...style
  }} {...rest}>{children}</div>
);

const Field = ({ label, hint, children, style = {} }) => (
  <label style={{ display: 'flex', flexDirection: 'column', gap: 6, ...style }}>
    {label && <span style={{ fontSize: 12, fontWeight: 500, color: 'var(--ink-600)', letterSpacing: '.01em' }}>{label}</span>}
    {children}
    {hint && <span style={{ fontSize: 11.5, color: 'var(--ink-400)' }}>{hint}</span>}
  </label>
);

const Input = ({ icon, suffix, style = {}, full, ...rest }) => (
  <div style={{
    display: 'flex', alignItems: 'center', gap: 8,
    background: '#fff', border: '1px solid var(--line-hard)', borderRadius: 8,
    padding: '0 12px', height: 40, width: full ? '100%' : 'auto',
    ...style
  }}>
    {icon && <Icon name={icon} size={16} color="var(--ink-400)"/>}
    <input style={{ flex: 1, border: 'none', outline: 'none', background: 'transparent', fontSize: 14, fontFamily: 'inherit', color: 'var(--ink-900)', minWidth: 0 }} {...rest}/>
    {suffix && <span style={{ color: 'var(--ink-400)', fontSize: 12.5 }}>{suffix}</span>}
  </div>
);

const Select = ({ options = [], value, onChange, style = {}, full }) => (
  <div style={{
    position: 'relative', display: 'inline-flex', alignItems: 'center',
    width: full ? '100%' : 'auto',
    background: '#fff', border: '1px solid var(--line-hard)', borderRadius: 8,
    height: 40, padding: '0 12px', paddingRight: 32, ...style
  }}>
    <select value={value} onChange={(e)=>onChange?.(e.target.value)} style={{
      width: '100%', appearance: 'none', border: 'none', outline: 'none', background: 'transparent',
      fontSize: 14, fontFamily: 'inherit', color: 'var(--ink-900)', cursor: 'pointer'
    }}>
      {options.map(o => typeof o === 'string'
        ? <option key={o} value={o}>{o}</option>
        : <option key={o.value} value={o.value}>{o.label}</option>)}
    </select>
    <span style={{ position: 'absolute', right: 10, pointerEvents: 'none' }}>
      <Icon name="chevron" size={14} color="var(--ink-400)"/>
    </span>
  </div>
);

const KV = ({ k, v, mono, strong, color, big, divider }) => (
  <div style={{
    display: 'flex', justifyContent: 'space-between', alignItems: 'baseline',
    padding: divider ? '12px 0' : '4px 0',
    borderTop: divider ? '1px dashed var(--line)' : 'none',
  }}>
    <span style={{ color: color || 'var(--ink-500)', fontSize: big ? 15 : 13 }}>{k}</span>
    <span className={mono ? "num" : ""} style={{
      color: color || 'var(--ink-900)',
      fontSize: big ? 22 : 14,
      fontWeight: strong || big ? 600 : 500,
      letterSpacing: big ? '-.01em' : 0,
    }}>{v}</span>
  </div>
);

const SectionTitle = ({ children, action, size = "md", style = {} }) => (
  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12, ...style }}>
    <h3 style={{
      margin: 0,
      fontSize: size === 'lg' ? 18 : 14, fontWeight: 600, color: 'var(--ink-800)',
      letterSpacing: size === 'lg' ? '-.01em' : '.01em',
      textTransform: size === 'lg' ? 'none' : 'uppercase',
    }}>{children}</h3>
    {action}
  </div>
);

// PKR formatter
const fmt = (n, opts = {}) => {
  const { withSign = false, currency = false } = opts;
  if (n == null || isNaN(n)) return "—";
  const num = Number(n);
  const sign = num < 0 ? "−" : (withSign && num > 0 ? "+" : "");
  const abs = Math.abs(num);
  const s = abs.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
  return currency ? `${sign}PKR ${s}` : `${sign}${s}`;
};
const fmtT = (n) => `PKR ${(Number(n)||0).toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;

// Image placeholder — striped SVG with mono label
const Placeholder = ({ label, ratio = "1/1", height, style = {} }) => {
  const id = `s-${(label||'').replace(/\W/g,'')}-${Math.random().toString(36).slice(2,6)}`;
  return (
    <div style={{ aspectRatio: height ? undefined : ratio, height, width: '100%', position: 'relative', overflow: 'hidden', borderRadius: 6, background: '#F6F7FB', ...style }}>
      <svg viewBox="0 0 100 100" preserveAspectRatio="xMidYMid slice" style={{ position: 'absolute', inset: 0, width: '100%', height: '100%' }}>
        <defs>
          <pattern id={id} patternUnits="userSpaceOnUse" width="8" height="8" patternTransform="rotate(45)">
            <line x1="0" y1="0" x2="0" y2="8" stroke="#DCE1EC" strokeWidth="2"/>
          </pattern>
        </defs>
        <rect width="100" height="100" fill={`url(#${id})`}/>
      </svg>
      {label && (
        <div style={{
          position: 'absolute', inset: 0, display: 'flex', alignItems: 'center', justifyContent: 'center',
          fontFamily: 'var(--mono)', fontSize: 9.5, color: 'var(--ink-500)', letterSpacing: '.04em',
          textTransform: 'uppercase', padding: 6, textAlign: 'center'
        }}>{label}</div>
      )}
    </div>
  );
};

// PIN pad shared
const PinPad = ({ onKey, large }) => {
  const keys = ["1","2","3","4","5","6","7","8","9","C","0","⌫"];
  return (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 10 }}>
      {keys.map(k => {
        const isAction = k === "C" || k === "⌫";
        return (
          <button key={k} onClick={() => onKey?.(k)} style={{
            height: large ? 72 : 60, borderRadius: 10,
            border: '1px solid ' + (isAction ? 'var(--line)' : 'var(--line-hard)'),
            background: isAction ? 'var(--surface-2)' : '#fff',
            color: isAction ? 'var(--ink-600)' : 'var(--ink-900)',
            fontSize: large ? 26 : 22, fontWeight: 500, fontFamily: 'var(--mono)',
            cursor: 'pointer', transition: 'background .1s'
          }}>{k}</button>
        );
      })}
    </div>
  );
};

// Status dot
const Dot = ({ tone = "neutral", size = 8 }) => {
  const c = { success: 'var(--green-600)', danger: 'var(--red-600)', amber: 'var(--amber-600)', neutral: 'var(--ink-400)', blue: 'var(--blue-600)' }[tone];
  return <span style={{ width: size, height: size, borderRadius: 99, background: c, display: 'inline-block', boxShadow: tone === 'success' ? `0 0 0 3px ${c}22` : 'none' }}/>;
};

// Table primitives
const Table = ({ columns, rows, dense, onRowClick, emptyText = "No records" }) => (
  <div style={{ background: '#fff', border: '1px solid var(--line)', borderRadius: 10, overflow: 'hidden' }}>
    <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
      <thead>
        <tr style={{ background: 'var(--surface-2)' }}>
          {columns.map((c, i) => (
            <th key={i} style={{
              textAlign: c.align || 'left', fontWeight: 500, color: 'var(--ink-500)',
              fontSize: 11.5, textTransform: 'uppercase', letterSpacing: '.04em',
              padding: dense ? '8px 12px' : '12px 16px',
              borderBottom: '1px solid var(--line)',
              width: c.width,
            }}>{c.label}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {rows.length === 0 && (
          <tr><td colSpan={columns.length} style={{ padding: 32, textAlign: 'center', color: 'var(--ink-400)' }}>{emptyText}</td></tr>
        )}
        {rows.map((r, ri) => (
          <tr key={ri} onClick={() => onRowClick?.(r)} style={{
            borderBottom: ri === rows.length - 1 ? 'none' : '1px solid var(--line-soft)',
            cursor: onRowClick ? 'pointer' : 'default',
          }}>
            {columns.map((c, ci) => (
              <td key={ci} style={{
                textAlign: c.align || 'left',
                padding: dense ? '8px 12px' : '14px 16px',
                color: c.muted ? 'var(--ink-500)' : 'var(--ink-900)',
                fontFamily: c.mono ? 'var(--mono)' : 'inherit',
                fontVariantNumeric: c.mono ? 'tabular-nums' : 'normal',
                whiteSpace: 'nowrap',
              }}>{c.render ? c.render(r) : r[c.key]}</td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  </div>
);

// Stat tile
const Stat = ({ label, value, delta, deltaTone = "success", sub, accent }) => (
  <div style={{
    background: '#fff', borderRadius: 12, border: '1px solid var(--line)',
    padding: 18, display: 'flex', flexDirection: 'column', gap: 8,
    position: 'relative', overflow: 'hidden',
  }}>
    {accent && <div style={{ position: 'absolute', left: 0, top: 0, bottom: 0, width: 3, background: accent }}/>}
    <div style={{ color: 'var(--ink-500)', fontSize: 12, fontWeight: 500, letterSpacing: '.04em', textTransform: 'uppercase' }}>{label}</div>
    <div className="num" style={{ fontSize: 28, fontWeight: 600, letterSpacing: '-.02em', color: 'var(--ink-900)' }}>{value}</div>
    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
      {delta && <Badge tone={deltaTone} dot>{delta}</Badge>}
      {sub && <span style={{ fontSize: 12, color: 'var(--ink-400)' }}>{sub}</span>}
    </div>
  </div>
);

// Modal shell
const Modal = ({ open, onClose, children, width = 520, style = {} }) => {
  if (!open) return null;
  return (
    <div style={{
      position: 'absolute', inset: 0, background: 'rgba(11,18,32,.5)',
      display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100,
      backdropFilter: 'blur(2px)',
    }} onClick={onClose}>
      <div onClick={(e) => e.stopPropagation()} style={{
        background: '#fff', borderRadius: 14, boxShadow: 'var(--shadow-3)',
        width, maxWidth: '92vw', maxHeight: '90vh', overflow: 'hidden',
        display: 'flex', flexDirection: 'column',
        ...style
      }}>{children}</div>
    </div>
  );
};

const ModalHeader = ({ title, subtitle, onClose, tone }) => (
  <div style={{
    padding: '18px 22px 16px',
    borderBottom: '1px solid var(--line)',
    display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 16,
    background: tone === 'amber' ? 'linear-gradient(180deg, var(--amber-50), #fff)' : tone === 'danger' ? 'linear-gradient(180deg, var(--red-50), #fff)' : '#fff'
  }}>
    <div>
      <h2 style={{ margin: 0, fontSize: 17, fontWeight: 600, letterSpacing: '-.01em' }}>{title}</h2>
      {subtitle && <div style={{ fontSize: 13, color: 'var(--ink-500)', marginTop: 4 }}>{subtitle}</div>}
    </div>
    {onClose && (
      <button onClick={onClose} style={{
        border: 'none', background: 'transparent', cursor: 'pointer', padding: 6, borderRadius: 6,
        color: 'var(--ink-500)'
      }}><Icon name="close" size={18}/></button>
    )}
  </div>
);

Object.assign(window, {
  Icon, Btn, Badge, Card, Field, Input, Select, KV, SectionTitle, fmt, fmtT,
  Placeholder, PinPad, Dot, Table, Stat, Modal, ModalHeader,
});
