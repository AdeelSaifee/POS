// App shell v4 — spec compliant
// Sidebar: 240px, hardware status in sidebar footer
// Top bar: 56px

const NAV = [
  { id:'checkout', label:'Checkout', icon:'cart' },
  { id:'orders',   label:'Orders',   icon:'receipt' },
  { id:'shift',    label:'Shift',    icon:'shift' },
  null,
  { id:'cash',     label:'Cash',     icon:'cash' },
  { id:'reports',  label:'Reports',  icon:'chart' },
  null,
  { id:'items',    label:'Items',    icon:'catalog' },
  { id:'people',   label:'People',   icon:'user' },
  { id:'admin',    label:'Admin',    icon:'admin' },
];

const HW_DEVICES = [
  { icon:'print',    label:'Printer',       ok:true  },
  { icon:'drawer',   label:'Cash drawer',   ok:true  },
  { icon:'scan',     label:'Scanner',       ok:true  },
  { icon:'terminal', label:'Card terminal', ok:true  },
  { icon:'sync',     label:'Network',       ok:true  },
];

// ── Sidebar — 240px per spec ──────────────────────────────────────────────────
const Sidebar = ({ active, onNav }) => {
  const isActive = id =>
    active === id
    || (id==='checkout' && ['payment','receipt'].includes(active))
    || (id==='cash'     && ['drawer-to-vault','vault-to-bank'].includes(active))
    || (id==='shift'    && active==='shift-close')
    || (id==='items'    && active==='catalog');

  return (
    <div style={{
      width:240, flexShrink:0, background:'#0F3A7D',
      display:'flex', flexDirection:'column',
      borderRight:'1px solid rgba(255,255,255,.06)',
    }}>
      {/* Logo */}
      <div style={{ padding:'14px 14px 12px', borderBottom:'1px solid rgba(255,255,255,.08)' }}>
        <div style={{
          background:'#fff', borderRadius:10, padding:'10px 16px',
          boxShadow:'0 2px 14px rgba(0,0,0,.3)',
        }}>
          <img src="assets/logo.png" style={{ width:'100%', display:'block' }}/>
        </div>
      </div>

      {/* Nav */}
      <div style={{ flex:1, overflow:'auto', padding:'10px 8px' }}>
        {NAV.map((n, i) => {
          if (n === null) return <div key={i} style={{ height:1, background:'rgba(255,255,255,.08)', margin:'6px 6px' }}/>;
          const ia = isActive(n.id);
          return (
            <button key={n.id} onClick={() => onNav(n.id)} style={{
              width:'100%', display:'flex', alignItems:'center', gap:11,
              padding:'10px 12px', borderRadius:8, border:'none',
              background: ia ? 'rgba(255,255,255,.12)' : 'transparent',
              color: ia ? '#fff' : 'rgba(255,255,255,.55)',
              cursor:'pointer', textAlign:'left', fontSize:14,
              fontWeight: ia ? 600 : 400, marginBottom:2,
              fontFamily:'inherit', transition:'background .1s, color .1s',
              position:'relative',
            }}
            onMouseEnter={e=>{ if(!ia){e.currentTarget.style.background='rgba(255,255,255,.06)'; e.currentTarget.style.color='rgba(255,255,255,.8)'; } }}
            onMouseLeave={e=>{ if(!ia){e.currentTarget.style.background='transparent'; e.currentTarget.style.color='rgba(255,255,255,.55)'; } }}
            >
              {/* Active left border */}
              {ia && <div style={{ position:'absolute',left:-8,top:8,bottom:8,width:4,background:'#fff',borderRadius:99 }}/>}
              <Icon name={n.icon} size={16}/>
              {n.label}
            </button>
          );
        })}
      </div>

      {/* Hardware status — spec: top-right of sidebar area, 12px */}
      <div style={{
        padding:'10px 14px', borderTop:'1px solid rgba(255,255,255,.08)',
        background:'rgba(0,0,0,.12)',
      }}>
        <div style={{ fontSize:9.5, color:'rgba(255,255,255,.3)', textTransform:'uppercase', letterSpacing:'.1em', fontWeight:600, marginBottom:8 }}>Hardware</div>
        <div style={{ display:'flex', flexWrap:'wrap', gap:'5px 12px' }}>
          {HW_DEVICES.map(d => (
            <div key={d.icon} title={d.label} style={{ display:'inline-flex', alignItems:'center', gap:5, fontSize:11, color:'rgba(255,255,255,.5)' }}>
              <Dot tone={d.ok?'success':'danger'} size={5}/>
              <Icon name={d.icon} size={12} color="rgba(255,255,255,.4)"/>
              <span>{d.label}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Operator */}
      <div style={{ padding:'10px 10px 14px' }}>
        <div style={{ display:'flex', alignItems:'center', gap:9, padding:9, borderRadius:8, background:'rgba(255,255,255,.06)' }}>
          <div style={{ width:30, height:30, borderRadius:'50%', background:'#F39C12', display:'grid', placeItems:'center', fontSize:12, fontWeight:700, color:'#fff', flexShrink:0 }}>AD</div>
          <div style={{ flex:1, minWidth:0 }}>
            <div style={{ fontSize:13, fontWeight:500, color:'rgba(255,255,255,.9)' }}>Adeel</div>
            <div style={{ fontSize:11, color:'rgba(255,255,255,.4)' }}>Cashier · E1042</div>
          </div>
        </div>
      </div>
    </div>
  );
};

// ── Top status bar — 56px per spec ───────────────────────────────────────────
const StatusBar = ({ onLock }) => {
  const [time, setTime] = React.useState(new Date());
  React.useEffect(() => {
    const t = setInterval(() => setTime(new Date()), 1000);
    return () => clearInterval(t);
  }, []);

  return (
    <div style={{
      height:56, background:'#F8F9FA',
      borderBottom:'1px solid #E1E4E8',
      display:'flex', alignItems:'center',
      padding:'0 20px', gap:14, flexShrink:0,
    }}>
      {/* Left — company */}
      <div style={{ display:'flex', alignItems:'center', gap:9 }}>
        <div style={{ width:28, height:28, borderRadius:6, background:'#0F3A7D', color:'#fff', display:'grid', placeItems:'center', fontSize:10, fontWeight:800 }}>RT</div>
        <span style={{ fontSize:14, fontWeight:700, color:'#2C3E50', letterSpacing:'-.01em' }}>R Technologies POS</span>
      </div>

      {/* Center — location · terminal · employee · shift */}
      <div style={{ flex:1, display:'flex', alignItems:'center', justifyContent:'center', gap:6, fontSize:12, color:'#7F8C8D' }}>
        <span style={{ fontWeight:500, color:'#4A5568' }}>Main Branch</span>
        <span>·</span>
        <span className="num" style={{ fontWeight:500, color:'#4A5568' }}>POS-01</span>
        <span>·</span>
        <span style={{ fontWeight:500, color:'#4A5568' }}>Adeel</span>
        <span>·</span>
        <div style={{ display:'inline-flex', alignItems:'center', gap:5, padding:'2px 8px', background:'#F0FDF4', borderRadius:99 }}>
          <Dot tone="success" size={5}/>
          <span style={{ fontSize:11.5, color:'#0A7A2B', fontWeight:500 }}>Shift open · 5h 42m</span>
        </div>
      </div>

      {/* Right — date/time + sync + lock */}
      <div style={{ display:'flex', alignItems:'center', gap:10 }}>
        <div className="num" style={{ fontSize:12, color:'#7F8C8D' }}>
          Tue 20 May 2026 · {time.toLocaleTimeString('en-GB', { hour:'2-digit', minute:'2-digit', second:'2-digit' })}
        </div>

        <div style={{ display:'inline-flex', alignItems:'center', gap:5, padding:'3px 9px', background:'#F0FDF4', borderRadius:6 }}>
          <Icon name="sync" size={12} color="#0A7A2B"/>
          <span style={{ fontSize:11.5, color:'#0A7A2B', fontWeight:500 }}>Online</span>
        </div>

        <button onClick={onLock} title="Lock terminal" style={{
          width:32, height:32, borderRadius:6,
          border:'1px solid #E1E4E8', background:'#fff',
          cursor:'pointer', display:'grid', placeItems:'center', color:'#95A5A6',
        }}>
          <Icon name="lock" size={13}/>
        </button>
      </div>
    </div>
  );
};

// ── Sub-tabs ──────────────────────────────────────────────────────────────────
const SubTabs = ({ items, active, onChange }) => (
  <div style={{ display:'flex', gap:0, padding:'0 24px', background:'#fff', borderBottom:'1px solid #E1E4E8', flexShrink:0 }}>
    {items.map(it => (
      <button key={it.id} onClick={() => onChange(it.id)} style={{
        padding:'12px 16px', border:'none', background:'transparent', cursor:'pointer',
        fontSize:14, fontWeight:500, fontFamily:'inherit',
        color: active === it.id ? '#0F3A7D' : '#7F8C8D',
        borderBottom:`2px solid ${active === it.id ? '#0F3A7D' : 'transparent'}`,
        marginBottom:-1, transition:'color .1s',
      }}>{it.label}</button>
    ))}
  </div>
);

// ── Root App ──────────────────────────────────────────────────────────────────
const App = () => {
  const [route,       setRoute]       = React.useState('account-login');
  const [loggedAcct,  setLoggedAcct]  = React.useState('Adeel');
  const [paymentCtx,  setPaymentCtx]  = React.useState({ total:3885, cart:EXAMPLE_CART, initialTender:'cash' });
  const [mgrModal,    setMgrModal]    = React.useState(null);
  const [voidModal,   setVoidModal]   = React.useState(null);
  const [reportTab,   setReportTab]   = React.useState('z');

  React.useEffect(() => {
    window.__posGo       = (r, acct) => { if (acct) setLoggedAcct(acct); setMgrModal(null); setVoidModal(null); setRoute(r); };
    window.__posReport   = tab        => { setReportTab(tab); setRoute('reports'); };
    window.__posShowMgr  = ()         => { setMgrModal('Price override — manager approval required'); setRoute('checkout'); };
    window.__posShowVoid = ()         => { setVoidModal(EXAMPLE_CART[1]); setRoute('checkout'); };
    window.__posTender   = tender     => {
      setPaymentCtx(c => ({ ...c, initialTender: tender }));
      setRoute('payment');
      setTimeout(() => { window.__setTender?.(tender); }, 300);
    };
  }, [setRoute, setLoggedAcct, setReportTab, setMgrModal, setVoidModal, setPaymentCtx]);

  const handleNav = id => {
    const map = { checkout:'checkout', orders:'held-orders', shift:'shift-close', cash:'cash', reports:'reports', items:'catalog', people:'people', admin:'admin' };
    setRoute(map[id] || id);
  };

  const renderScreen = () => {
    switch (route) {
      case 'account-login': return <AccountLoginScreen onLogin={acc => { setLoggedAcct(acc||'Adeel'); setRoute('pin-login'); }}/>;
      case 'pin-login':     return <PinLoginScreen employeeName={loggedAcct||'Adeel'} onPinLogin={() => setRoute('shift-open')} onLogout={() => setRoute('account-login')}/>;
      case 'shift-open':    return <ShiftOpenScreen onOpen={() => setRoute('checkout')} onCancel={() => setRoute('pin-login')}/>;

      case 'checkout': return <CheckoutScreen
        goPayment={(t,c) => { setPaymentCtx({total:t,cart:c}); setRoute('payment'); }}
        openManager={a => setMgrModal(a||'Manager override')}
        openVoid={l => setVoidModal(l)}
      />;
      case 'payment':  return <PaymentScreen total={paymentCtx.total} cart={paymentCtx.cart} initialTender={paymentCtx.initialTender||'cash'} onComplete={() => setRoute('receipt')} onBack={() => setRoute('checkout')}/>;
      case 'receipt':  return <ReceiptScreen onNew={() => setRoute('checkout')}/>;

      case 'cash':           return <CashSection setRoute={setRoute} setMgrModal={setMgrModal}/>;
      case 'drawer-to-vault':return <DrawerToVaultSection setRoute={setRoute} setMgrModal={setMgrModal}/>;
      case 'vault-to-bank':  return <VaultToBankSection  setRoute={setRoute} setMgrModal={setMgrModal}/>;

      case 'shift-close': return <ShiftCloseScreen onClose={() => setRoute('pin-login')} onBack={() => setRoute('checkout')}/>;

      case 'reports': return (
        <div style={{ height:'100%', display:'flex', flexDirection:'column' }}>
          <SubTabs active={reportTab} onChange={setReportTab} items={[{id:'z',label:'Z-Report'},{id:'daily',label:'Daily sales'},{id:'employee',label:'Employee-wise'}]}/>
          <div style={{ flex:1, overflow:'hidden' }}>
            {reportTab==='z'       && <ZReportScreen/>}
            {reportTab==='daily'   && <DailySalesReport/>}
            {reportTab==='employee'&& <EmployeeReport/>}
          </div>
        </div>
      );

      case 'catalog':      return <ItemManagementScreen/>;
      case 'people':       return <EmployeeTerminalScreen/>;
      case 'admin':        return <AdminDashboard go={setRoute}/>;
      case 'returns':      return <ReturnsScreen/>;
      case 'held-orders':  return <HeldOrdersScreen onResume={() => setRoute('checkout')}/>;
      default: return null;
    }
  };

  const fullscreen = ['account-login','pin-login'].includes(route);

  return (
    <>
      {fullscreen ? renderScreen() : (
        <div style={{ display:'flex', width:'100%', height:'100%', overflow:'hidden' }}>
          <Sidebar active={route} onNav={handleNav}/>
          <div style={{ flex:1, display:'flex', flexDirection:'column', minWidth:0 }}>
            <StatusBar onLock={() => setRoute('pin-login')}/>
            <div style={{ flex:1, overflow:'hidden', position:'relative' }}>
              {renderScreen()}
            </div>
          </div>
        </div>
      )}

      <ManagerApprovalModal open={!!mgrModal} action={mgrModal} onClose={() => setMgrModal(null)} onApprove={() => setMgrModal(null)} onReject={() => setMgrModal(null)}/>
      <VoidItemModal open={!!voidModal} line={voidModal} onClose={() => setVoidModal(null)} onConfirm={() => setVoidModal(null)} onRequestManager={() => { setVoidModal(null); setMgrModal('Void item · manager approval required'); }}/>
      <PrototypeNav route={route} setRoute={setRoute} setLoggedAcct={setLoggedAcct}/>
    </>
  );
};

// Cash section wrappers
const cashTabs = [
  {id:'drawer',label:'Cash Drawer'},
  {id:'drawer-to-vault',label:'Drawer → Vault'},
  {id:'vault-to-bank',  label:'Vault → Bank'},
];
const CashSection = ({ setRoute, setMgrModal }) => (
  <div style={{ height:'100%', display:'flex', flexDirection:'column' }}>
    <SubTabs active="drawer" onChange={id => setRoute(id==='drawer'?'cash':id)} items={cashTabs}/>
    <div style={{ flex:1, overflow:'hidden' }}><CashDrawerScreen onTransfer={() => setRoute('drawer-to-vault')}/></div>
  </div>
);
const DrawerToVaultSection = ({ setRoute, setMgrModal }) => (
  <div style={{ height:'100%', display:'flex', flexDirection:'column' }}>
    <SubTabs active="drawer-to-vault" onChange={id => setRoute(id==='drawer'?'cash':id)} items={cashTabs}/>
    <div style={{ flex:1, overflow:'hidden' }}><DrawerToVaultScreen onBack={() => setRoute('cash')} onConfirm={() => setMgrModal('Drawer → Vault')}/></div>
  </div>
);
const VaultToBankSection = ({ setRoute, setMgrModal }) => (
  <div style={{ height:'100%', display:'flex', flexDirection:'column' }}>
    <SubTabs active="vault-to-bank" onChange={id => setRoute(id==='drawer'?'cash':id)} items={cashTabs}/>
    <div style={{ flex:1, overflow:'hidden' }}><VaultToBankScreen onBack={() => setRoute('cash')} onConfirm={() => setMgrModal('Bank deposit')}/></div>
  </div>
);

// ── Prototype navigator ───────────────────────────────────────────────────────
const PrototypeNav = ({ route, setRoute, setLoggedAcct }) => {
  const [open, setOpen] = React.useState(false);
  const screens = [
    {id:'account-login',   label:'1. Account / password login'},
    {id:'pin-login',       label:'2. PIN login'},
    {id:'shift-open',      label:'3. Shift open'},
    {id:'checkout',        label:'4. Checkout · catalog · cart'},
    {id:'held-orders',     label:'5. Held orders'},
    {id:'payment',         label:'6. Payment — Cash'},
    {id:'receipt',         label:'7. Receipt'},
    {id:'returns',         label:'8. Returns & refunds'},
    {id:'cash',            label:'9. Cash drawer'},
    {id:'drawer-to-vault', label:'10. Drawer → Vault'},
    {id:'vault-to-bank',   label:'11. Vault → Bank'},
    {id:'shift-close',     label:'12. Shift close'},
    {id:'reports',         label:'13–15. Reports'},
    {id:'catalog',         label:'16. Item management'},
    {id:'people',          label:'17. People & devices'},
    {id:'admin',           label:'18. Admin dashboard'},
  ];
  return (
    <div style={{ position:'fixed', bottom:16, right:16, zIndex:9999 }}>
      {open && (
        <div style={{
          background:'#fff', borderRadius:12, boxShadow:'var(--shadow-3)',
          width:310, padding:6, marginBottom:8, border:'1px solid #E1E4E8',
          maxHeight:500, overflow:'auto',
        }}>
          <div style={{ padding:'7px 10px 5px', fontSize:10, color:'#7F8C8D', textTransform:'uppercase', letterSpacing:'.08em', fontWeight:600 }}>Screens</div>
          {screens.map(s => (
            <button key={s.id} onClick={() => { setLoggedAcct?.('Adeel'); setRoute(s.id); setOpen(false); }} style={{
              width:'100%', padding:'8px 10px', border:'none', borderRadius:6, cursor:'pointer',
              textAlign:'left', fontFamily:'inherit', fontSize:12.5,
              background: route===s.id ? '#EFF6FF' : 'transparent',
              color: route===s.id ? '#0F3A7D' : '#2C3E50',
              fontWeight: route===s.id ? 600 : 400,
              display:'flex', alignItems:'center', gap:8,
            }}>
              {route===s.id ? <Dot tone="blue"/> : <span style={{ width:8 }}/>}
              {s.label}
            </button>
          ))}
        </div>
      )}
      <button onClick={() => setOpen(o=>!o)} style={{
        height:38, padding:'0 15px', background:'#0F3A7D', color:'#fff',
        border:'none', borderRadius:99, cursor:'pointer', boxShadow:'var(--shadow-2)',
        display:'inline-flex', alignItems:'center', gap:7, fontSize:13, fontWeight:600, fontFamily:'inherit',
      }}>
        <Icon name="grid" size={13}/> {open?'Close':'Screens'}
      </button>
    </div>
  );
};

ReactDOM.createRoot(document.getElementById('app')).render(<App/>);
