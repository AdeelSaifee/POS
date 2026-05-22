// Extra screens: Returns / Refund, Held Orders

// ── Returns / Refund screen ───────────────────────────────────────────────────
const ReturnsScreen = () => {
  const [query, setQuery] = React.useState('MB-260520-00483');
  const [found, setFound] = React.useState(true);
  const [selected, setSelected] = React.useState({ 'I1001': false, 'I1008': true, 'I1015': false });

  const orderLines = [
    { id:'I1001', name:'Milk 1L',        qty:2, price:280,  unit:'L' },
    { id:'I1008', name:'Cola 1.5L',      qty:2, price:220,  unit:'L' },
    { id:'I1015', name:'Tea Pack 500g',  qty:1, price:950,  unit:'g' },
    { id:'I1002', name:'Rice 5kg',       qty:1, price:1850, unit:'kg'},
  ];

  const selectedTotal = orderLines
    .filter(l => selected[l.id])
    .reduce((s,l) => s + l.qty*l.price, 0);

  return (
    <div style={{ padding:28, height:'100%', overflow:'auto', background:'#F8F9FA' }}>
      <div style={{ maxWidth:1080, margin:'0 auto' }}>
        <div style={{ marginBottom:20 }}>
          <div style={{ fontSize:11, color:'#7F8C8D', letterSpacing:'.08em', textTransform:'uppercase', fontWeight:600 }}>Operations</div>
          <h1 style={{ margin:'4px 0 0', fontSize:26, fontWeight:700, letterSpacing:'-.02em', color:'#2C3E50' }}>Returns & Refunds</h1>
          <p style={{ margin:'4px 0 0', color:'#7F8C8D', fontSize:14 }}>Search original order, select items to return, issue refund.</p>
        </div>

        <div style={{ display:'grid', gridTemplateColumns:'1fr 360px', gap:22, alignItems:'start' }}>
          <div style={{ display:'flex', flexDirection:'column', gap:16 }}>
            {/* Order lookup */}
            <Card padding={20}>
              <SectionTitle>Find original order</SectionTitle>
              <div style={{ display:'flex', gap:10 }}>
                <div style={{ flex:1, display:'flex', alignItems:'center', height:48, background:'#F8F9FA', border:'1px solid #E1E4E8', borderRadius:8, padding:'0 14px', gap:10 }}>
                  <Icon name="search" size={16} color="#7F8C8D"/>
                  <input value={query} onChange={e=>setQuery(e.target.value)}
                    placeholder="Receipt #, Order ID, or barcode…"
                    style={{ flex:1, border:'none', outline:'none', background:'transparent', fontSize:15, fontFamily:'inherit', color:'#2C3E50' }}/>
                </div>
                <button onClick={()=>setFound(true)} style={{ height:48, padding:'0 20px', background:'#0F3A7D', color:'#fff', border:'none', borderRadius:8, fontFamily:'inherit', fontWeight:600, fontSize:14, cursor:'pointer' }}>
                  Search
                </button>
              </div>
            </Card>

            {found && (
              <Card padding={0}>
                {/* Order header */}
                <div style={{ padding:'14px 18px', borderBottom:'1px solid #E1E4E8', display:'flex', justifyContent:'space-between', alignItems:'center' }}>
                  <div>
                    <div className="num" style={{ fontWeight:700, fontSize:15, color:'#2C3E50' }}>MB-260520-00483</div>
                    <div style={{ fontSize:12, color:'#7F8C8D', marginTop:3 }}>20 May 2026 · 14:42 · Adeel · POS-01 · Main Branch</div>
                  </div>
                  <Badge tone="success" dot>Paid — PKR 3,885</Badge>
                </div>

                {/* OrderLines with checkboxes */}
                <div style={{ padding:'8px 0' }}>
                  {orderLines.map(l => (
                    <div key={l.id} onClick={() => setSelected(s=>({...s,[l.id]:!s[l.id]}))} style={{
                      padding:'12px 18px', display:'flex', alignItems:'center', gap:14,
                      borderBottom:'1px solid rgba(0,0,0,.04)', cursor:'pointer',
                      background: selected[l.id] ? 'rgba(231,76,60,.03)' : '#fff',
                      transition:'background .1s',
                    }}>
                      <div style={{
                        width:20, height:20, borderRadius:5, flexShrink:0,
                        border:`2px solid ${selected[l.id]?'#E74C3C':'#E1E4E8'}`,
                        background: selected[l.id]?'#E74C3C':'#fff',
                        display:'grid', placeItems:'center',
                      }}>
                        {selected[l.id] && <Icon name="check" size={12} color="#fff"/>}
                      </div>
                      <div style={{ flex:1 }}>
                        <div style={{ fontWeight:600, fontSize:14, color:'#2C3E50' }}>{l.name}</div>
                        <div className="num" style={{ fontSize:12, color:'#7F8C8D', marginTop:2 }}>{l.qty} × {fmtT(l.price)} = {fmtT(l.qty*l.price)}</div>
                      </div>
                      <Badge tone={selected[l.id]?'danger':'outline'}>{selected[l.id]?'Return':'Keep'}</Badge>
                    </div>
                  ))}
                </div>

                <div style={{ padding:'12px 18px', background:'#F8F9FA', borderTop:'1px solid #E1E4E8', display:'flex', justifyContent:'space-between', alignItems:'center' }}>
                  <div style={{ fontSize:13, color:'#7F8C8D' }}>{Object.values(selected).filter(Boolean).length} item{Object.values(selected).filter(Boolean).length!==1?'s':''} selected for return</div>
                  <span className="num" style={{ fontSize:16, fontWeight:700, color:'#E74C3C' }}>Refund: {fmtT(selectedTotal)}</span>
                </div>
              </Card>
            )}
          </div>

          {/* Right: return summary */}
          <div style={{ display:'flex', flexDirection:'column', gap:14 }}>
            <Card padding={20}>
              <SectionTitle>Return summary</SectionTitle>
              <div style={{ display:'flex', flexDirection:'column', gap:6 }}>
                {orderLines.filter(l=>selected[l.id]).map(l=>(
                  <div key={l.id} style={{ display:'flex', justifyContent:'space-between', fontSize:13 }}>
                    <span style={{ color:'#2C3E50' }}>{l.name} ×{l.qty}</span>
                    <span className="num" style={{ fontWeight:600, color:'#E74C3C' }}>{fmtT(l.qty*l.price)}</span>
                  </div>
                ))}
                {selectedTotal === 0 && <div style={{ color:'#BDC3C7', fontSize:13, textAlign:'center', padding:'12px 0' }}>No items selected</div>}
              </div>

              {selectedTotal > 0 && (
                <>
                  <div style={{ marginTop:14, padding:'12px 0 0', borderTop:'1px solid #E1E4E8', display:'flex', justifyContent:'space-between', alignItems:'baseline' }}>
                    <span style={{ fontSize:14, fontWeight:500, color:'#7F8C8D' }}>Refund amount</span>
                    <span className="num" style={{ fontSize:24, fontWeight:700, color:'#E74C3C' }}>{fmtT(selectedTotal)}</span>
                  </div>
                </>
              )}
            </Card>

            <Card padding={16} style={{ background:'#FEF3C7', borderColor:'#FDE68A' }}>
              <div style={{ display:'flex', gap:9 }}>
                <Icon name="warning" size={16} color="#B45309"/>
                <div style={{ fontSize:12.5, color:'#92400E', lineHeight:1.5 }}>
                  <strong>Manager approval required</strong><br/>
                  Refunds above PKR 500 need manager PIN before processing.
                </div>
              </div>
            </Card>

            <Field label="Refund method">
              <Select full value="Cash refund" options={['Cash refund','Original payment method','Store credit']} onChange={()=>{}}/>
            </Field>

            <Field label="Reason code">
              <Select full value="Customer changed mind" options={REASON_CODES} onChange={()=>{}}/>
            </Field>

            <Btn variant="danger" size="xl" full icon="arrowL" disabled={selectedTotal===0}>
              Process Refund {selectedTotal>0?fmtT(selectedTotal):''}
            </Btn>
            <Btn variant="amber" icon="user" full>Request manager approval</Btn>
            <Btn variant="default" full>Cancel</Btn>
          </div>
        </div>
      </div>
    </div>
  );
};

// ── Held Orders screen ────────────────────────────────────────────────────────
const HeldOrdersScreen = ({ onResume }) => {
  const heldOrders = [
    { id:'HOLD-001', ref:'ORD-MB-...00481', items:5, total:2840, time:'14:22', customer:'Ahmed Khan', cashier:'Adeel', note:'Customer left to get wallet' },
    { id:'HOLD-002', ref:'ORD-MB-...00478', items:2, total:650,  time:'14:05', customer:'Guest',       cashier:'Adeel', note:'Price check requested' },
    { id:'HOLD-003', ref:'ORD-MB-...00469', items:8, total:5120, time:'13:41', customer:'Sara Baig',   cashier:'Bilal', note:'Manager approval pending' },
  ];

  return (
    <div style={{ padding:28, height:'100%', overflow:'auto', background:'#F8F9FA' }}>
      <div style={{ maxWidth:900, margin:'0 auto' }}>
        <div style={{ marginBottom:20 }}>
          <div style={{ fontSize:11, color:'#7F8C8D', letterSpacing:'.08em', textTransform:'uppercase', fontWeight:600 }}>Orders</div>
          <h1 style={{ margin:'4px 0 0', fontSize:26, fontWeight:700, letterSpacing:'-.02em', color:'#2C3E50' }}>Held orders</h1>
          <p style={{ margin:'4px 0 0', color:'#7F8C8D', fontSize:14 }}>Resume a paused transaction or discard it.</p>
        </div>

        <div style={{ display:'flex', flexDirection:'column', gap:12 }}>
          {heldOrders.map(o => (
            <Card key={o.id} padding={20} style={{ display:'grid', gridTemplateColumns:'1fr auto', gap:16, alignItems:'center' }}>
              <div style={{ display:'grid', gridTemplateColumns:'1fr 1fr', gap:'6px 24px' }}>
                <div>
                  <div className="num" style={{ fontWeight:700, fontSize:15, color:'#2C3E50' }}>{o.ref}</div>
                  <div style={{ fontSize:12, color:'#7F8C8D', marginTop:2 }}>{o.time} · {o.cashier} · POS-01</div>
                </div>
                <div className="num" style={{ fontSize:22, fontWeight:700, color:'#0F3A7D', textAlign:'right' }}>{fmtT(o.total)}</div>
                <div style={{ display:'flex', alignItems:'center', gap:8, fontSize:13 }}>
                  <Icon name="user" size={13} color="#7F8C8D"/>
                  <span style={{ color:'#4A5568', fontWeight:500 }}>{o.customer}</span>
                  <span style={{ color:'#BDC3C7' }}>·</span>
                  <span style={{ color:'#7F8C8D' }}>{o.items} items</span>
                </div>
                <div style={{ fontSize:12.5, color:'#7F8C8D', fontStyle:'italic' }}>"{o.note}"</div>
              </div>
              <div style={{ display:'flex', flexDirection:'column', gap:8 }}>
                <Btn variant="primary" size="md" onClick={() => onResume?.(o)}>Resume order</Btn>
                <Btn variant="dangerOutline" size="sm">Discard</Btn>
              </div>
            </Card>
          ))}

          {heldOrders.length === 0 && (
            <div style={{ padding:60, textAlign:'center', color:'#BDC3C7' }}>
              <Icon name="receipt" size={36} color="#E1E4E8"/>
              <div style={{ marginTop:12, fontSize:14, color:'#7F8C8D' }}>No orders currently on hold.</div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

Object.assign(window, { ReturnsScreen, HeldOrdersScreen });
