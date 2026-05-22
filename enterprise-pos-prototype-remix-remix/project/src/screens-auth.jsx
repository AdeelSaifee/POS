// Auth screens v4 — full spec implementation
// Account login: 50/50 split, navy left + white form right
// PIN login: single 440px centered card, 6-digit, 80×64 keypad
// Lock / logout flow

// ── Dark navy background ──────────────────────────────────────────────────────
const LoginBG = () => (
  <div style={{ position:'absolute', inset:0, zIndex:0 }}>
    <div style={{ position:'absolute', inset:0, background:'linear-gradient(140deg,#04101F 0%,#091E3D 60%,#060D1E 100%)' }}/>
    <div style={{ position:'absolute', top:'-10%', right:'-5%', width:500, height:500, borderRadius:'50%', background:'radial-gradient(circle,rgba(15,58,125,.18) 0%,transparent 70%)' }}/>
    <div style={{ position:'absolute', bottom:'-8%', left:'5%',  width:400, height:400, borderRadius:'50%', background:'radial-gradient(circle,rgba(243,156,18,.1) 0%,transparent 70%)' }}/>
  </div>
);

// ── Account / Password Login — 50/50 split ────────────────────────────────────
const AccountLoginScreen = ({ onLogin }) => {
  const [account,  setAccount]  = React.useState('');
  const [password, setPassword] = React.useState('');
  const [showPw,   setShowPw]   = React.useState(false);
  const [loading,  setLoading]  = React.useState(false);
  const [error,    setError]    = React.useState('');
  const [focused,  setFocused]  = React.useState('');

  const handleLogin = () => {
    if (!account || !password) { setError('Please enter your account and password.'); return; }
    setError(''); setLoading(true);
    setTimeout(() => { setLoading(false); onLogin?.(account); }, 700);
  };

  const inputBox = (id) => ({
    display:'flex', alignItems:'center', height:48, gap:10,
    padding:'0 14px',
    background: focused === id ? '#fff' : '#F8F9FA',
    border: `1px solid ${focused === id ? '#0F3A7D' : '#E1E4E8'}`,
    borderRadius:8,
    boxShadow: focused === id ? '0 0 0 3px rgba(15,58,125,.12)' : 'none',
    transition:'all .15s',
  });

  return (
    <div style={{ position:'absolute', inset:0, display:'flex', alignItems:'center', justifyContent:'center' }}>
      <LoginBG/>

      <div style={{
        position:'relative', zIndex:10,
        display:'flex', width:'100%', maxWidth:960, height:580,
        borderRadius:16,
        overflow:'hidden',
        boxShadow:'0 40px 80px -20px rgba(0,0,0,.7), 0 12px 32px rgba(0,0,0,.4)',
      }}>
        {/* ── LEFT: navy brand panel (50%) ── */}
        <div style={{
          flex:1,
          background:'linear-gradient(160deg,#0A1D3D 0%,#0F3A7D 100%)',
          display:'flex', flexDirection:'column',
          alignItems:'center', justifyContent:'center',
          padding:'40px 48px',
          position:'relative', overflow:'hidden',
        }}>
          {/* Gold hairline top */}
          <div style={{ position:'absolute', top:0, left:'15%', right:'15%', height:2, background:'linear-gradient(90deg,transparent,rgba(243,156,18,.5),transparent)' }}/>

          {/* Logo in white card */}
          <div style={{
            background:'#fff', borderRadius:12, padding:'20px 28px',
            boxShadow:'0 8px 32px rgba(0,0,0,.4)',
            marginBottom:28, width:'100%', maxWidth:260,
          }}>
            <img src="assets/logo.png" style={{ width:'100%', display:'block' }}/>
          </div>

          <h2 style={{ margin:'0 0 8px', fontSize:22, fontWeight:700, color:'#fff', textAlign:'center', letterSpacing:'-.01em' }}>R Technologies POS</h2>
          <p style={{ margin:0, fontSize:13.5, color:'rgba(255,255,255,.55)', textAlign:'center', lineHeight:1.5 }}>Enterprise multi-location POS platform</p>

          {/* Hardware dots */}
          <div style={{
            marginTop:36, padding:'14px 18px',
            background:'rgba(255,255,255,.05)', borderRadius:10,
            border:'1px solid rgba(255,255,255,.08)', width:'100%',
          }}>
            <div style={{ fontSize:9.5, color:'rgba(255,255,255,.3)', textTransform:'uppercase', letterSpacing:'.1em', fontWeight:600, marginBottom:10 }}>Terminal status</div>
            <div style={{ display:'grid', gridTemplateColumns:'1fr 1fr', gap:'6px 14px' }}>
              {[
                {icon:'print',    label:'Printer',        ok:true},
                {icon:'drawer',   label:'Cash drawer',    ok:true},
                {icon:'scan',     label:'Scanner',        ok:true},
                {icon:'terminal', label:'Card terminal',  ok:true},
                {icon:'sync',     label:'Network',        ok:true},
              ].map(d => (
                <div key={d.label} style={{ display:'flex', alignItems:'center', gap:7, fontSize:12 }}>
                  <Dot tone={d.ok?'success':'danger'} size={5}/>
                  <Icon name={d.icon} size={13} color="rgba(255,255,255,.4)"/>
                  <span style={{ color:'rgba(255,255,255,.6)' }}>{d.label}</span>
                </div>
              ))}
            </div>
          </div>

          <div style={{ position:'absolute', bottom:20, display:'flex', gap:14 }}>
            <button style={{ background:'none',border:'none',color:'rgba(255,255,255,.28)',cursor:'pointer',fontFamily:'inherit',fontSize:11.5 }}>User Agreement</button>
            <span style={{ color:'rgba(255,255,255,.15)' }}>|</span>
            <button style={{ background:'none',border:'none',color:'rgba(255,255,255,.28)',cursor:'pointer',fontFamily:'inherit',fontSize:11.5 }}>Privacy Policy</button>
          </div>
        </div>

        {/* ── RIGHT: white form panel (50%) ── */}
        <div style={{
          flex:1, background:'#fff',
          display:'flex', flexDirection:'column', justifyContent:'center',
          padding:'48px 52px',
        }}>
          <h1 style={{ margin:'0 0 6px', fontSize:24, fontWeight:700, color:'#2C3E50', letterSpacing:'-.01em' }}>Account Login</h1>
          <p style={{ margin:'0 0 28px', fontSize:14, color:'#7F8C8D' }}>Sign in to your POS account</p>

          {/* Account field */}
          <div style={{ marginBottom:14 }}>
            <div style={{ fontSize:12, fontWeight:500, color:'#4A5568', marginBottom:6, letterSpacing:'.01em' }}>Account or Email</div>
            <div style={inputBox('account')} onFocus={()=>setFocused('account')} onBlur={()=>setFocused('')} tabIndex={-1}>
              <Icon name="user" size={16} color="#BDC3C7"/>
              <input
                value={account} onChange={e=>{setAccount(e.target.value);setError('');}}
                onKeyDown={e=>e.key==='Enter'&&handleLogin()}
                onFocus={()=>setFocused('account')} onBlur={()=>setFocused('')}
                placeholder="Account or Email"
                style={{ flex:1,border:'none',outline:'none',background:'transparent',fontSize:15,fontFamily:'inherit',color:'#2C3E50' }}
                autoFocus
              />
            </div>
          </div>

          {/* Password field */}
          <div style={{ marginBottom:error?10:20 }}>
            <div style={{ fontSize:12, fontWeight:500, color:'#4A5568', marginBottom:6, letterSpacing:'.01em' }}>Password</div>
            <div style={inputBox('password')} onFocus={()=>setFocused('password')} onBlur={()=>setFocused('')} tabIndex={-1}>
              <Icon name="lock" size={16} color="#BDC3C7"/>
              <input
                type={showPw?'text':'password'}
                value={password} onChange={e=>{setPassword(e.target.value);setError('');}}
                onKeyDown={e=>e.key==='Enter'&&handleLogin()}
                onFocus={()=>setFocused('password')} onBlur={()=>setFocused('')}
                placeholder="Password"
                style={{ flex:1,border:'none',outline:'none',background:'transparent',fontSize:15,fontFamily:'inherit',color:'#2C3E50' }}
              />
              <button onClick={()=>setShowPw(s=>!s)} style={{ border:'none',background:'transparent',cursor:'pointer',padding:2,color:'#BDC3C7',display:'grid',placeItems:'center' }}>
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  {showPw?<><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19M1 1l22 22"/></>:<><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></>}
                </svg>
              </button>
            </div>
          </div>

          {/* Error */}
          {error && (
            <div style={{ marginBottom:14, padding:'8px 12px', background:'#FEF2F2', border:'1px solid #FECACA', borderRadius:6, fontSize:13, color:'#C0392B', display:'flex', gap:7, alignItems:'center' }}>
              <Icon name="warning" size={14} color="#C0392B"/>{error}
            </div>
          )}

          {/* Login button */}
          <button onClick={handleLogin} disabled={loading}
            style={{
              width:'100%', height:48, borderRadius:8, border:'none', marginBottom:16,
              background: loading?'#7F8C8D':'#0F3A7D',
              color:'#fff', fontSize:16, fontWeight:700, fontFamily:'inherit',
              cursor:loading?'wait':'pointer',
              display:'flex', alignItems:'center', justifyContent:'center', gap:8,
              transition:'background .15s',
              boxShadow:'0 4px 12px rgba(15,58,125,.25)',
            }}>
            {loading
              ? <><svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2.5" strokeLinecap="round"><path d="M21 12a9 9 0 1 1-6.22-8.56"/></svg>Signing in…</>
              : 'Login'}
          </button>

          <div style={{ textAlign:'center', marginBottom:32 }}>
            <button style={{ background:'none',border:'none',color:'#0F3A7D',fontSize:14,cursor:'pointer',fontFamily:'inherit',textDecoration:'underline',textUnderlineOffset:3 }}>
              How to get an account to login?
            </button>
          </div>

          {/* Terminal info footer */}
          <div style={{ padding:'10px 14px', background:'#F8F9FA', borderRadius:8, textAlign:'center' }}>
            <div className="num" style={{ fontSize:12, color:'#7F8C8D', lineHeight:1.6 }}>
              POS-01 · Main Branch<br/>R Technologies POS · v8.4
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

// ── PIN Login — single 440px centered card ────────────────────────────────────
const PinLoginScreen = ({ employeeName = 'Adeel', onPinLogin, onLogout }) => {
  const [pin,     setPin]    = React.useState('');
  const [shake,   setShake]  = React.useState(false);
  const [showAtt, setShowAtt]= React.useState(false);
  const LEN = 6;

  // 3×4 grid: 7 8 9 / 4 5 6 / 1 2 3 / Clear 0 ⌫
  const KEYS = ['7','8','9','4','5','6','1','2','3','Clear','0','⌫'];

  const handleKey = k => {
    if (k === 'Clear') { setPin(''); return; }
    if (k === '⌫') { setPin(p => p.slice(0,-1)); return; }
    if (pin.length >= LEN) return;
    const next = pin + k;
    setPin(next);
    if (next.length === LEN) {
      setTimeout(() => {
        if (next === '000000') {
          setShake(true); setPin('');
          setTimeout(() => setShake(false), 500);
        } else {
          onPinLogin?.();
        }
      }, 200);
    }
  };

  return (
    <div style={{ position:'absolute', inset:0, display:'flex', alignItems:'center', justifyContent:'center' }}>
      <LoginBG/>

      {/* Single centered card — 440px */}
      <div style={{
        position:'relative', zIndex:10,
        width:440, background:'#fff', borderRadius:16,
        boxShadow:'0 40px 80px -20px rgba(0,0,0,.7), 0 12px 32px rgba(0,0,0,.4)',
        padding:'28px 32px 28px',
        display:'flex', flexDirection:'column', gap:0,
      }}>
        {/* Attendance button — top right */}
        <div style={{ display:'flex', justifyContent:'flex-end', marginBottom:16 }}>
          <button onClick={()=>setShowAtt(true)} style={{
            display:'inline-flex', alignItems:'center', gap:6,
            height:30, padding:'0 12px', fontFamily:'inherit',
            border:'1px solid #E1E4E8', borderRadius:99,
            background:'#fff', fontSize:12, fontWeight:500, color:'#4A5568',
            cursor:'pointer',
          }}>
            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>
            </svg>
            Attendance
          </button>
        </div>

        {/* Employee name */}
        <div style={{ textAlign:'center', marginBottom:20 }}>
          <div style={{ fontSize:22, fontWeight:700, color:'#2C3E50', letterSpacing:'-.01em' }}>{employeeName}</div>
          <div style={{ fontSize:12, color:'#7F8C8D', marginTop:4 }}>PIN Login · Cashier · Main Branch</div>
        </div>

        {/* 6 circular PIN boxes */}
        <div style={{
          display:'flex', justifyContent:'center', gap:10, marginBottom:20,
          animation: shake ? 'shake .4s ease' : 'none',
        }}>
          {Array.from({length:LEN}).map((_,i) => (
            <div key={i} style={{
              width:44, height:44, borderRadius:'50%',
              border: `2px solid ${shake ? '#E74C3C' : i < pin.length ? '#0F3A7D' : '#E1E4E8'}`,
              background: i < pin.length ? '#0F3A7D' : '#F8F9FA',
              display:'grid', placeItems:'center',
              transition:'all .12s',
              boxShadow: i < pin.length ? '0 4px 12px rgba(15,58,125,.2)' : 'none',
            }}>
              {i < pin.length && <div style={{ width:12, height:12, borderRadius:'50%', background:'#fff' }}/>}
            </div>
          ))}
        </div>

        {/* 3×4 keypad — spec: 80×64px buttons, 8px gap */}
        <div style={{ display:'grid', gridTemplateColumns:'repeat(3,1fr)', gap:8 }}>
          {KEYS.map(k => {
            const isClear = k === 'Clear';
            const isDel   = k === '⌫';
            return (
              <button key={k} onClick={() => handleKey(k)} style={{
                height:64,
                borderRadius:8,
                border:'1px solid #E1E4E8',
                background: isClear ? '#FEF2F2' : '#fff',
                color: isClear ? '#E74C3C' : isDel ? '#4A5568' : '#2C3E50',
                fontSize: k.match(/^\d$/) ? 24 : 14,
                fontWeight: k.match(/^\d$/) ? 500 : 600,
                fontFamily: k.match(/^\d$/) ? 'var(--mono)' : 'inherit',
                cursor:'pointer',
                display:'flex', alignItems:'center', justifyContent:'center',
                boxShadow:'0 1px 3px rgba(0,0,0,.05)',
                transition:'background .08s, transform .06s',
              }}
              onMouseDown={e => e.currentTarget.style.transform='scale(.94)'}
              onMouseUp={e   => e.currentTarget.style.transform='scale(1)'}
              onMouseLeave={e=> e.currentTarget.style.transform='scale(1)'}
              >
                {isDel
                  ? <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M21 4H8L2 12l6 8h13a2 2 0 0 0 2-2V6a2 2 0 0 0-2-2z"/>
                      <line x1="18" y1="9" x2="13" y2="14"/><line x1="13" y1="9" x2="18" y2="14"/>
                    </svg>
                  : k}
              </button>
            );
          })}
        </div>

        {/* Log out + help */}
        <div style={{ marginTop:20, display:'flex', justifyContent:'space-between', alignItems:'center' }}>
          <button onClick={onLogout} style={{ background:'none',border:'none',color:'#0F3A7D',fontSize:14,cursor:'pointer',fontFamily:'inherit',fontWeight:500 }}>
            Log out
          </button>
          <button style={{ background:'none',border:'none',color:'#7F8C8D',fontSize:12.5,cursor:'pointer',fontFamily:'inherit' }}>
            How to get a PIN code to login?
          </button>
        </div>
      </div>

      <style>{`@keyframes shake{0%,100%{transform:translateX(0)}20%,60%{transform:translateX(-6px)}40%,80%{transform:translateX(6px)}}`}</style>

      {/* Attendance modal */}
      <Modal open={showAtt} onClose={() => setShowAtt(false)} width={360}>
        <ModalHeader title="Attendance" subtitle={`${employeeName} · ${new Date().toLocaleDateString('en-GB',{dateStyle:'long'})}`} onClose={() => setShowAtt(false)}/>
        <div style={{ padding:20, display:'flex', flexDirection:'column', gap:10 }}>
          <Btn variant="success" size="lg" full icon="check" onClick={() => setShowAtt(false)}>Clock In</Btn>
          <Btn variant="default" size="lg" full onClick={() => setShowAtt(false)}>Clock Out</Btn>
        </div>
      </Modal>
    </div>
  );
};

// ── Shift open ────────────────────────────────────────────────────────────────
const ShiftOpenScreen = ({ onOpen, onCancel }) => {
  const [opening, setOpening] = React.useState('15000');
  const dens = [5000,1000,500,100,50,20,10,5];
  const [dc, setDc] = React.useState({});
  const total = Object.entries(dc).reduce((s,[d,c])=>s+Number(d)*Number(c||0),0);

  return (
    <div style={{ padding:24, height:'100%', overflow:'auto', background:'var(--surface)' }}>
      <div style={{ maxWidth:1040, margin:'0 auto' }}>
        <div style={{ marginBottom:20 }}>
          <div style={{ fontSize:11, color:'var(--ink-500)', letterSpacing:'.08em', textTransform:'uppercase', fontWeight:600 }}>Shift</div>
          <h1 style={{ margin:'4px 0 0', fontSize:28, fontWeight:700, letterSpacing:'-.02em', color:'var(--ink-900)' }}>Open shift · POS-01</h1>
          <p style={{ margin:'4px 0 0', color:'var(--ink-500)', fontSize:14 }}>Declare opening cash for <span className="num">20 May 2026</span>.</p>
        </div>
        <div style={{ display:'grid', gridTemplateColumns:'1fr 340px', gap:20 }}>
          <Card padding={22}>
            <SectionTitle>Denomination count</SectionTitle>
            <div style={{ display:'grid', gridTemplateColumns:'repeat(4,1fr)', gap:10, marginBottom:14 }}>
              {dens.map(d => (
                <div key={d} style={{ border:'1px solid var(--line)', borderRadius:8, padding:12 }}>
                  <div className="num" style={{ fontSize:12.5, color:'var(--ink-500)', marginBottom:8 }}>PKR {d.toLocaleString()}</div>
                  <div style={{ display:'flex', alignItems:'center', gap:6 }}>
                    <button onClick={()=>setDc(c=>({...c,[d]:Math.max(0,(Number(c[d])||0)-1)}))} style={{ width:26,height:26,borderRadius:6,border:'1px solid var(--line)',background:'#fff',cursor:'pointer' }}><Icon name="minus" size={12}/></button>
                    <input className="num" value={dc[d]||''} onChange={e=>setDc(c=>({...c,[d]:e.target.value.replace(/\D/g,'')}))} placeholder="0" style={{ flex:1,height:30,border:'1px solid var(--line-hard)',borderRadius:6,padding:'0 6px',fontSize:14,fontFamily:'var(--mono)',textAlign:'center',outline:'none' }}/>
                    <button onClick={()=>setDc(c=>({...c,[d]:(Number(c[d])||0)+1}))} style={{ width:26,height:26,borderRadius:6,border:'1px solid var(--line)',background:'#fff',cursor:'pointer' }}><Icon name="plus" size={12}/></button>
                  </div>
                </div>
              ))}
            </div>
            <Field label="Or enter total (PKR)" style={{ marginBottom:12 }}><Input full value={opening} onChange={e=>setOpening(e.target.value)}/></Field>
          </Card>
          <div style={{ display:'flex', flexDirection:'column', gap:14 }}>
            <Card padding={18}>
              <div style={{ fontSize:11, color:'var(--ink-500)', textTransform:'uppercase', letterSpacing:'.05em', fontWeight:600 }}>Opening cash</div>
              <div className="num" style={{ fontSize:36,fontWeight:700,letterSpacing:'-.02em',marginTop:4,color:'var(--ink-900)' }}>{fmtT(total||opening)}</div>
              <div style={{ marginTop:12,paddingTop:10,borderTop:'1px dashed var(--line)',fontSize:12.5,display:'flex',justifyContent:'space-between' }}>
                <span style={{ color:'var(--ink-500)' }}>Expected</span><span className="num">PKR 15,000</span>
              </div>
            </Card>
            <Card padding={14} style={{ background:'var(--amber-50)',borderColor:'var(--amber-100)' }}>
              <div style={{ display:'flex',gap:9 }}><Icon name="warning" size={16} color="var(--amber-700)"/><div style={{ fontSize:12.5,color:'var(--amber-700)',lineHeight:1.5 }}>Cash will be locked until manager signs in.</div></div>
            </Card>
            <Btn variant="success" size="xl" full onClick={onOpen}><Icon name="check" size={17}/> Open shift</Btn>
            <Btn variant="default" size="md" full onClick={onCancel}>Cancel</Btn>
          </div>
        </div>
      </div>
    </div>
  );
};

// ── Shift close ───────────────────────────────────────────────────────────────
const ShiftCloseScreen = ({ onClose: onCloseShift, onBack }) => {
  const [counted, setCounted] = React.useState('48750');
  const expected = 48885;
  const variance = Number(counted||0) - expected;

  return (
    <div style={{ padding:24, height:'100%', overflow:'auto', background:'var(--surface)' }}>
      <button onClick={onBack} style={{ background:'transparent',border:'none',cursor:'pointer',color:'var(--ink-500)',fontSize:13,display:'inline-flex',alignItems:'center',gap:5,marginBottom:14,fontFamily:'inherit' }}>
        <Icon name="chevronL" size={15}/> Back
      </button>
      <div style={{ marginBottom:18 }}>
        <div style={{ fontSize:11,color:'var(--ink-500)',letterSpacing:'.08em',textTransform:'uppercase',fontWeight:600 }}>Shift S-4218 · Adeel · POS-01</div>
        <h1 style={{ margin:'4px 0 0',fontSize:26,fontWeight:700,letterSpacing:'-.02em',color:'var(--ink-900)' }}>Close shift</h1>
      </div>
      <div style={{ display:'grid',gridTemplateColumns:'1fr 1fr',gap:20,marginBottom:20 }}>
        <Card padding={22}>
          <SectionTitle>Drawer reconciliation</SectionTitle>
          <KV k="Opening cash"      v="PKR 15,000"  mono/>
          <KV k="Cash sales"        v="+ PKR 38,200" mono color="var(--green-700)"/>
          <KV k="Refunds (cash)"    v="− PKR 1,200"  mono color="var(--red-700)"/>
          <KV k="Payouts"           v="− PKR 800"    mono color="var(--red-700)"/>
          <KV k="Drawer → Vault"    v="− PKR 2,315"  mono color="var(--ink-500)"/>
          <KV k="Expected drawer"   v="PKR 48,885"   mono strong big divider/>
          <Field label="Counted cash" style={{ marginTop:16 }}><Input full value={counted} onChange={e=>setCounted(e.target.value.replace(/\D/g,''))}/></Field>
          <div style={{ marginTop:14,padding:13,borderRadius:8,background:variance===0?'var(--green-50)':variance<0?'var(--red-50)':'var(--amber-50)',display:'flex',alignItems:'center',justifyContent:'space-between' }}>
            <div style={{ display:'flex',alignItems:'center',gap:9 }}>
              <Icon name={variance===0?'check':'warning'} size={17} color={variance===0?'var(--green-700)':variance<0?'var(--red-700)':'var(--amber-700)'}/>
              <span style={{ fontSize:13,color:variance===0?'var(--green-700)':variance<0?'var(--red-700)':'var(--amber-700)' }}>{variance===0?'Reconciled — no variance':variance<0?`Short ${fmtT(Math.abs(variance))}`:`Over ${fmtT(variance)}`}</span>
            </div>
            <span className="num" style={{ fontSize:17,fontWeight:700,color:variance===0?'var(--green-700)':variance<0?'var(--red-700)':'var(--amber-700)' }}>{variance>=0?'+':'−'}PKR {Math.abs(variance).toLocaleString()}</span>
          </div>
        </Card>
        <Card padding={22}>
          <SectionTitle>Shift summary</SectionTitle>
          <div style={{ display:'grid',gridTemplateColumns:'1fr 1fr',gap:10,marginBottom:14 }}>
            <Stat label="Orders" value="184" sub="2 voided"/>
            <Stat label="Gross sales" value="PKR 86,580"/>
            <Stat label="Tax" value="PKR 4,210"/>
            <Stat label="Approvals" value="6" sub="3 by Fahad"/>
          </div>
          <SectionTitle>Tender mix</SectionTitle>
          {[{k:'Cash',v:38200,c:'var(--green-600)'},{k:'Card',v:42150,c:'var(--blue-600)'},{k:'Wallet',v:4980,c:'var(--amber-600)'},{k:'Voucher',v:1250,c:'var(--ink-400)'}].map(t=>{
            const pct=(t.v/86580)*100;
            return <div key={t.k} style={{ marginBottom:10 }}><div style={{ display:'flex',justifyContent:'space-between',fontSize:12.5,marginBottom:4 }}><span>{t.k}</span><span className="num">{fmtT(t.v)} · {pct.toFixed(1)}%</span></div><div style={{ height:6,background:'var(--surface-2)',borderRadius:99 }}><div style={{ height:'100%',width:`${pct}%`,background:t.c,borderRadius:99 }}/></div></div>;
          })}
        </Card>
      </div>
      <div style={{ display:'flex',gap:10,justifyContent:'flex-end' }}>
        <Btn variant="default" icon="print">Print summary</Btn>
        <Btn variant="amber" icon="user">Request manager</Btn>
        <Btn variant="primary" icon="check" size="lg" onClick={onCloseShift}>Close shift</Btn>
      </div>
    </div>
  );
};

Object.assign(window, { AccountLoginScreen, PinLoginScreen, ShiftOpenScreen, ShiftCloseScreen });
