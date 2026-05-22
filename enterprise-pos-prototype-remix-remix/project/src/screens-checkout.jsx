// Checkout screens v4 — full spec implementation
// Item cards 240×220, 65/35 split, cash-received inline in cart, Pay 48px

// ── Category color map ────────────────────────────────────────────────────────
const CAT_COLORS = {
  all:        { bg:'#EEF2FF', fg:'#4F46E5', icon:'◈' },
  grocery:    { bg:'#ECFDF5', fg:'#059669', icon:'◉' },
  dairy:      { bg:'#EFF6FF', fg:'#2563EB', icon:'◎' },
  beverage:   { bg:'#FFF7ED', fg:'#D97706', icon:'◐' },
  personal:   { bg:'#FAF5FF', fg:'#7C3AED', icon:'◑' },
  pharmacy:   { bg:'#F0FDFA', fg:'#0D9488', icon:'✚' },
  electron:   { bg:'#F5F3FF', fg:'#6D28D9', icon:'◆' },
  stationery: { bg:'#FEFCE8', fg:'#B45309', icon:'◇' },
  service:    { bg:'#FFF1F2', fg:'#BE123C', icon:'★' },
};

// ── Item Card — spec: 240×220, image 240×120, Add button full-width 40px ─────
const ItemTile = ({ item, onAdd }) => {
  const c = CAT_COLORS[item.cat] || CAT_COLORS.all;
  const [hov, setHov] = React.useState(false);

  return (
    <button
      onMouseEnter={() => setHov(true)}
      onMouseLeave={() => setHov(false)}
      style={{
        width:240, flexShrink:0,
        background:'#fff',
        border:`0.5px solid ${hov ? '#0F3A7D' : '#E1E4E8'}`,
        borderRadius:8, overflow:'hidden',
        cursor:'pointer', textAlign:'left', padding:0,
        display:'flex', flexDirection:'column',
        boxShadow: hov ? '0 4px 12px rgba(0,0,0,.1)' : '0 2px 4px rgba(0,0,0,.05)',
        transform: hov ? 'translateY(-2px)' : 'none',
        transition:'all .15s',
      }}>

      {/* Image area — spec: 240×120, light gray #E8E8E8, centered icon */}
      <div style={{
        height:120, background:'#E8E8E8',
        display:'flex', alignItems:'center', justifyContent:'center',
        position:'relative',
        flexShrink:0,
      }}>
        <div style={{
          width:56, height:56, borderRadius:14,
          background: c.bg, display:'flex', alignItems:'center', justifyContent:'center',
        }}>
          <span style={{ fontSize:26, fontWeight:700, color:c.fg }}>
            {item.name[0]}
          </span>
        </div>
        {/* Low stock hint — spec: 12px red, only if low */}
        {item.stock < 20 && (
          <div style={{
            position:'absolute', top:8, right:8,
            padding:'3px 7px', borderRadius:4,
            background:'rgba(231,76,60,.1)', color:'#E74C3C',
            fontSize:11.5, fontWeight:500,
          }}>
            {item.stock} left
          </div>
        )}
      </div>

      {/* Content — spec: 12px padding */}
      <div style={{ padding:'10px 12px 0', flex:1 }}>
        {/* Item name — spec: 14px bold navy */}
        <div style={{
          fontSize:14, fontWeight:700, color:'#2C3E50',
          overflow:'hidden', textOverflow:'ellipsis', whiteSpace:'nowrap',
          marginBottom:4,
        }}>
          {item.name}
        </div>

        {/* Price — spec: 20px bold navy */}
        <div style={{ display:'flex', alignItems:'baseline', justifyContent:'space-between' }}>
          <span className="num" style={{ fontSize:20, fontWeight:700, color:'#0F3A7D' }}>
            {fmtT(item.price)}
          </span>
          {/* Unit — spec: 12px gray right-aligned */}
          <span style={{ fontSize:12, color:'#7F8C8D' }}>per {item.unit}</span>
        </div>
      </div>

      {/* Add to Cart button — spec: full-width, 40px, navy bg, white text */}
      <button
        onClick={e => { e.stopPropagation(); onAdd(item); }}
        style={{
          margin:'10px 12px 12px', height:40,
          background: hov ? '#163D85' : '#0F3A7D',
          color:'#fff', border:'none', borderRadius:6,
          fontSize:14, fontWeight:600, fontFamily:'inherit',
          cursor:'pointer', transition:'background .12s',
          display:'flex', alignItems:'center', justifyContent:'center', gap:6,
        }}>
        <Icon name="plus" size={14}/> Add to Cart
      </button>
    </button>
  );
};

// ── OrderLine in cart — spec: 60px base, qty controls, unit price, total, remove ──
const CartLine = ({ line, onQty, onRemove, selected, onSelect }) => (
  <div onClick={onSelect} style={{
    padding:'10px 16px',
    display:'grid', gridTemplateColumns:'1fr auto',
    gap:'4px 12px', alignItems:'center',
    borderBottom:'0.5px solid #E1E4E8',
    cursor:'pointer',
    background: selected ? 'rgba(15,58,125,.04)' : '#fff',
    borderLeft:`3px solid ${selected ? '#0F3A7D' : 'transparent'}`,
    minHeight:60,
    transition:'background .1s',
  }}>
    {/* Name + unit price */}
    <div style={{ minWidth:0 }}>
      <div style={{ fontSize:14, fontWeight:700, color:'#2C3E50', overflow:'hidden', textOverflow:'ellipsis', whiteSpace:'nowrap' }}>
        {line.name}
      </div>
      <div className="num" style={{ fontSize:12, color:'#7F8C8D', marginTop:2 }}>
        {fmtT(line.price)} per {line.unit}
      </div>
    </div>

    {/* Right: qty controls + total + remove */}
    <div style={{ display:'flex', alignItems:'center', gap:7, flexShrink:0 }}>
      {/* Qty − n + */}
      <button onClick={e => { e.stopPropagation(); onQty(line.id, -1); }} style={{
        width:28, height:28, borderRadius:6,
        border:'1px solid #E1E4E8', background:'#fff',
        cursor:'pointer', display:'grid', placeItems:'center',
        color:'#4A5568',
      }}>
        <Icon name="minus" size={11}/>
      </button>
      <span className="num" style={{ fontSize:14, fontWeight:700, minWidth:20, textAlign:'center', color:'#2C3E50' }}>{line.qty}</span>
      <button onClick={e => { e.stopPropagation(); onQty(line.id, 1); }} style={{
        width:28, height:28, borderRadius:6,
        border:'1px solid #E1E4E8', background:'#fff',
        cursor:'pointer', display:'grid', placeItems:'center',
        color:'#4A5568',
      }}>
        <Icon name="plus" size={11}/>
      </button>

      {/* Line total — spec: 14px bold navy */}
      <span className="num" style={{ fontSize:14, fontWeight:700, color:'#0F3A7D', minWidth:76, textAlign:'right' }}>
        {fmtT(line.qty * line.price)}
      </span>

      {/* Remove — spec: small red ✕ */}
      <button
        onClick={e => { e.stopPropagation(); onRemove?.(line); }}
        style={{ border:'none', background:'transparent', cursor:'pointer', color:'#BDC3C7', fontSize:12, padding:'2px 4px', borderRadius:4, transition:'color .1s' }}
        onMouseEnter={e => e.currentTarget.style.color='#E74C3C'}
        onMouseLeave={e => e.currentTarget.style.color='#BDC3C7'}
      >
        ✕
      </button>
    </div>
  </div>
);

// ── Manager approval modal ────────────────────────────────────────────────────
const ManagerApprovalModal = ({ open, action, onApprove, onReject, onClose }) => {
  const [pin, setPin] = React.useState('');
  const [reason, setReason] = React.useState(REASON_CODES[0]);
  const [comment, setComment] = React.useState('');
  return (
    <Modal open={open} onClose={onClose} width={560}>
      <ModalHeader title="Manager Approval Required" subtitle={action} onClose={onClose} tone="amber"/>
      <div style={{ padding:22, display:'grid', gridTemplateColumns:'1fr 1fr', gap:20 }}>
        <div>
          <Field label="Reason code" style={{ marginBottom:14 }}>
            <Select full value={reason} onChange={setReason} options={REASON_CODES}/>
          </Field>
          <Field label="Additional notes">
            <textarea value={comment} onChange={e => setComment(e.target.value)}
              placeholder="Additional notes (optional)"
              style={{ width:'100%', minHeight:80, border:'1px solid #E1E4E8', borderRadius:6, padding:10, fontSize:13, fontFamily:'inherit', resize:'vertical', outline:'none' }}/>
          </Field>
          <div style={{ marginTop:12, padding:12, background:'#F8F9FA', borderRadius:6, fontSize:11.5, color:'#7F8C8D' }}>
            <div style={{ fontWeight:600, color:'#4A5568', marginBottom:3 }}>Audit record</div>
            Operator E1042 · POS-01 · Main Branch<br/>
            <span className="num">20 May 2026 14:42 · crl-7f3a-8b22</span>
          </div>
        </div>
        <div>
          <Field label="Requested by">
            <Select full value="E1043 — Fahad · Manager" options={['E1043 — Fahad · Manager','E1046 — Hira · Manager (remote)']} onChange={() => {}}/>
          </Field>
          <Field label="Manager PIN" style={{ marginTop:14 }}>
            <div style={{ display:'flex', justifyContent:'center', gap:8, margin:'10px 0' }}>
              {Array.from({ length:4 }).map((_,i) => (
                <div key={i} style={{ width:36, height:42, borderRadius:7, border:`1.5px solid ${i<pin.length?'#0F3A7D':'#E1E4E8'}`, background:i<pin.length?'#EFF6FF':'#fff', display:'grid', placeItems:'center', fontSize:18, fontWeight:600 }}>
                  {i < pin.length ? '●' : ''}
                </div>
              ))}
            </div>
            <PinPad onKey={k => { if (k==='C') setPin(''); else if (k==='⌫') setPin(p=>p.slice(0,-1)); else if (pin.length<4) setPin(p=>p+k); }}/>
          </Field>
        </div>
      </div>
      <div style={{ padding:'14px 22px', borderTop:'1px solid #E1E4E8', display:'flex', justifyContent:'center', gap:12, background:'#F8F9FA' }}>
        <Btn variant="ghost" onClick={onClose}>Cancel</Btn>
        <Btn variant="danger" onClick={onReject}>Reject</Btn>
        <Btn variant="success" icon="check" disabled={pin.length < 4} onClick={onApprove}>Approve</Btn>
      </div>
    </Modal>
  );
};

// ── Void item modal ───────────────────────────────────────────────────────────
const VoidItemModal = ({ open, line, onConfirm, onClose, onRequestManager }) => {
  const [reason, setReason] = React.useState(REASON_CODES[0]);
  if (!line) return null;
  return (
    <Modal open={open} onClose={onClose} width={480}>
      <ModalHeader title="Remove / Void Item" subtitle="Append-only event — original line stays in the ledger." onClose={onClose} tone="danger"/>
      <div style={{ padding:22 }}>
        <div style={{ display:'flex', gap:12, alignItems:'center', padding:14, background:'#F8F9FA', borderRadius:8, marginBottom:16 }}>
          <div style={{ width:44, height:44, borderRadius:8, background:'#EEF2FF', display:'grid', placeItems:'center', fontSize:20, fontWeight:700, color:'#4F46E5' }}>{line.name[0]}</div>
          <div style={{ flex:1 }}>
            <div style={{ fontWeight:700, fontSize:14, color:'#2C3E50' }}>{line.name}</div>
            <div className="num" style={{ fontSize:12, color:'#7F8C8D', marginTop:2 }}>{line.qty} × {fmtT(line.price)} = {fmtT(line.qty*line.price)}</div>
          </div>
          <Badge tone="danger" dot>Void</Badge>
        </div>
        <Field label="Reason code" style={{ marginBottom:12 }}>
          <Select full value={reason} onChange={setReason} options={REASON_CODES}/>
        </Field>
        <div style={{ marginTop:10, padding:11, background:'#FFFBEB', border:'1px solid #FEF3C7', borderRadius:6, fontSize:12.5, color:'#C47D0E', display:'flex', gap:8, alignItems:'center' }}>
          <Icon name="warning" size={14} color="#C47D0E"/>
          Items above PKR 1,000 require manager approval.
        </div>
      </div>
      <div style={{ padding:'14px 22px', borderTop:'1px solid #E1E4E8', display:'flex', justifyContent:'flex-end', gap:8, background:'#F8F9FA' }}>
        <Btn variant="ghost" onClick={onClose}>Cancel</Btn>
        <Btn variant="amber" icon="user" onClick={onRequestManager}>Request manager</Btn>
        <Btn variant="danger" icon="trash" onClick={onConfirm}>Void item</Btn>
      </div>
    </Modal>
  );
};

// ── Main checkout screen — spec: 65% catalog / 35% cart ──────────────────────
const CheckoutScreen = ({ goPayment, openManager, openVoid }) => {
  const [cat,      setCat]      = React.useState('all');
  const [query,    setQuery]    = React.useState('');
  const [cart,     setCart]     = React.useState(EXAMPLE_CART.map(l => ({ ...l })));
  const [selected, setSelected] = React.useState(null);
  const [customer, setCustomer] = React.useState(null);
  const [cashRcvd, setCashRcvd]  = React.useState('');

  const filtered = ITEMS.filter(i =>
    (cat === 'all' || i.cat === cat) &&
    (!query || i.name.toLowerCase().includes(query.toLowerCase()) || i.sku.toLowerCase().includes(query.toLowerCase()))
  );

  const addItem = item => setCart(c => {
    const ex = c.find(l => l.id === item.id);
    if (ex) return c.map(l => l.id === item.id ? { ...l, qty: l.qty+1 } : l);
    return [...c, { id:item.id, name:item.name, qty:1, unit:item.unit, price:item.price }];
  });
  const changeQty = (id, d) => setCart(c => c.map(l => l.id===id ? {...l, qty:Math.max(1,l.qty+d)} : l));

  const subtotal = cart.reduce((s,l) => s + l.qty * l.price, 0);
  const discount = 100;
  const tax      = Math.round((subtotal - discount) * 0.05);
  const total    = subtotal - discount + tax;
  const change   = Math.max(0, Number(cashRcvd||0) - total);
  const orderNum = 'Order #4218';

  return (
    <div style={{ display:'grid', gridTemplateColumns:'65fr 35fr', height:'100%', overflow:'hidden' }}>

      {/* ── LEFT 65% — catalog ─────────────────────────────────────────────── */}
      <div style={{ display:'flex', flexDirection:'column', background:'#F8F9FA', borderRight:'1px solid #E1E4E8', overflow:'hidden' }}>

        {/* Search — spec: 48px tall, navy focus border + blue ring */}
        <div style={{ padding:'14px 18px', background:'#fff', borderBottom:'1px solid #E1E4E8' }}>
          <div style={{
            display:'flex', alignItems:'center', gap:10,
            height:48, background:'#F8F9FA',
            border:'1px solid #E1E4E8', borderRadius:8, padding:'0 16px',
          }}>
            <Icon name="search" size={18} color="#7F8C8D"/>
            <input
              value={query} onChange={e => setQuery(e.target.value)}
              placeholder="Scan barcode or search item…"
              style={{ flex:1, border:'none', outline:'none', background:'transparent', fontSize:16, fontFamily:'inherit', color:'#2C3E50' }}
              onFocus={e => { e.currentTarget.parentElement.style.borderColor='#0F3A7D'; e.currentTarget.parentElement.style.boxShadow='0 0 0 3px rgba(15,58,125,.12)'; }}
              onBlur={e  => { e.currentTarget.parentElement.style.borderColor='#E1E4E8'; e.currentTarget.parentElement.style.boxShadow='none'; }}
            />
            {query
              ? <button onClick={()=>setQuery('')} style={{border:'none',background:'transparent',cursor:'pointer',color:'#BDC3C7'}}><Icon name="close" size={14}/></button>
              : <div style={{display:'flex',alignItems:'center',gap:6,color:'#BDC3C7'}}>
                  <Icon name="scan" size={16} color="#BDC3C7"/>
                  <kbd style={{background:'#fff',border:'1px solid #E1E4E8',borderRadius:4,padding:'1px 5px',fontFamily:'var(--mono)',fontSize:10.5,color:'#7F8C8D'}}>F2</kbd>
                </div>
            }
          </div>
        </div>

        {/* Category chips — spec: 36px tall, pill, scrollable */}
        <div style={{ padding:'10px 18px', background:'#fff', borderBottom:'1px solid #E1E4E8', display:'flex', gap:8, overflowX:'auto' }}>
          {CATEGORIES.map(c => (
            <button key={c.id} onClick={() => setCat(c.id)} style={{
              height:36, padding:'0 16px', borderRadius:18, cursor:'pointer',
              whiteSpace:'nowrap', fontFamily:'inherit', fontWeight:500, fontSize:14,
              border:'none',
              background: cat===c.id ? '#0F3A7D' : '#F8F9FA',
              color: cat===c.id ? '#fff' : '#2C3E50',
              transition:'all .12s',
            }}>{c.name}</button>
          ))}
        </div>

        {/* Item grid — scrollable, auto-fill */}
        <div style={{ flex:1, overflow:'auto', padding:18 }}>
          {query && (
            <div style={{ marginBottom:10, fontSize:13, color:'#7F8C8D' }}>
              {filtered.length} result{filtered.length!==1?'s':''} for "<strong style={{color:'#2C3E50'}}>{query}</strong>"
            </div>
          )}
          {/* Scrollable horizontal rows that wrap — spec: 3 columns, each 240px */}
          <div style={{ display:'flex', flexWrap:'wrap', gap:12 }}>
            {filtered.map(item => <ItemTile key={item.id} item={item} onAdd={addItem}/>)}
          </div>
        </div>
      </div>

      {/* ── RIGHT 35% — cart / order summary ──────────────────────────────── */}
      <div style={{ display:'flex', flexDirection:'column', background:'#fff', overflow:'hidden' }}>

        {/* Order header */}
        <div style={{ padding:'12px 16px', borderBottom:'1px solid #E1E4E8' }}>
          <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between', marginBottom:8 }}>
            <span style={{ fontSize:16, fontWeight:700, color:'#2C3E50' }}>{orderNum}</span>
            <Badge tone="blue" dot>Open</Badge>
          </div>
          {/* Customer section */}
          {customer ? (
            <div style={{ display:'flex', alignItems:'center', gap:8, padding:'7px 10px', background:'#EFF6FF', borderRadius:6, fontSize:13 }}>
              <Icon name="user" size={13} color="#0F3A7D"/>
              <span style={{ flex:1, fontWeight:500, color:'#0F3A7D' }}>{customer}</span>
              <button onClick={() => setCustomer(null)} style={{ border:'none',background:'transparent',cursor:'pointer',color:'#93C5FD' }}><Icon name="close" size={12}/></button>
            </div>
          ) : (
            <button onClick={() => setCustomer('Ahmed Khan · +92 300 1234567')} style={{
              width:'100%', height:32, background:'#F8F9FA',
              border:'1px dashed #E1E4E8', borderRadius:6,
              color:'#7F8C8D', cursor:'pointer', fontSize:13, fontFamily:'inherit',
              display:'inline-flex', alignItems:'center', justifyContent:'center', gap:6,
            }}>
              <Icon name="plus" size={13}/> Guest / customer
            </button>
          )}
        </div>

        {/* OrderLines — spec: scrollable, 60px base row */}
        <div style={{ flex:1, overflow:'auto', minHeight:0 }}>
          {cart.length === 0 && (
            <div style={{ padding:48, textAlign:'center', color:'#BDC3C7' }}>
              <Icon name="cart" size={36} color="#E1E4E8"/>
              <div style={{ marginTop:12, fontSize:14, color:'#7F8C8D' }}>Scan or tap to add items.</div>
            </div>
          )}
          {cart.map(l => (
            <CartLine key={l.id} line={l}
              onQty={changeQty}
              selected={selected===l.id}
              onSelect={() => setSelected(s => s===l.id ? null : l.id)}
              onRemove={line => { if (line.price >= 1000) openVoid?.(line); else setCart(c=>c.filter(x=>x.id!==line.id)); }}
            />
          ))}
        </div>

        {/* Line actions */}
        {selected && (
          <div style={{ padding:'8px 14px', background:'#F8F9FA', borderTop:'1px solid #E1E4E8', display:'flex', gap:8 }}>
            <Btn variant="default" size="sm" icon="edit">Edit price</Btn>
            <Btn variant="default" size="sm" icon="minus">Discount</Btn>
            <div style={{ flex:1 }}/>
            <Btn variant="dangerOutline" size="sm" icon="trash" onClick={() => openVoid?.(cart.find(l=>l.id===selected))}>Void</Btn>
          </div>
        )}

        {/* Totals — spec: subtotal/tax/discount 14px gray, total 24px bold navy */}
        <div style={{ padding:'14px 16px', borderTop:'1px solid #E1E4E8', background:'#F8F9FA' }}>
          <div style={{ display:'flex', justifyContent:'space-between', fontSize:14, color:'#7F8C8D', marginBottom:4 }}>
            <span>Subtotal</span><span className="num">{fmtT(subtotal)}</span>
          </div>
          <div style={{ display:'flex', justifyContent:'space-between', fontSize:14, color:'#7F8C8D', marginBottom:4 }}>
            <span>Tax (5%)</span><span className="num">{fmtT(tax)}</span>
          </div>
          <div style={{ display:'flex', justifyContent:'space-between', fontSize:14, color:'#E74C3C', marginBottom:10 }}>
            <span>Discount</span><span className="num">− {fmtT(discount)}</span>
          </div>

          {/* Total due — spec: 24px bold navy */}
          <div style={{ display:'flex', justifyContent:'space-between', alignItems:'baseline', paddingTop:10, borderTop:'1px solid #E1E4E8', marginBottom:14 }}>
            <span style={{ fontSize:14, fontWeight:500, color:'#7F8C8D' }}>Total Due</span>
            <span className="num" style={{ fontSize:24, fontWeight:700, color:'#0F3A7D' }}>{fmtT(total)}</span>
          </div>

          {/* Cash received input — spec: 44px tall, 16px, navy border, auto-change */}
          <div style={{ display:'grid', gridTemplateColumns:'1fr 1fr', gap:10, marginBottom:14 }}>
            <div>
              <div style={{ fontSize:12, color:'#7F8C8D', marginBottom:5, fontWeight:500 }}>Cash Received</div>
              <div style={{ display:'flex', alignItems:'center', height:44, background:'#fff', border:'1px solid #E1E4E8', borderRadius:6, padding:'0 12px', gap:6 }}>
                <span style={{ fontSize:12, color:'#7F8C8D' }}>PKR</span>
                <input
                  className="num"
                  value={cashRcvd}
                  onChange={e => setCashRcvd(e.target.value.replace(/\D/g,''))}
                  placeholder="0"
                  style={{ flex:1, border:'none', outline:'none', background:'transparent', fontSize:16, fontFamily:'var(--mono)', color:'#2C3E50', fontWeight:600 }}
                />
              </div>
            </div>
            <div>
              <div style={{ fontSize:12, color:'#7F8C8D', marginBottom:5, fontWeight:500 }}>Change</div>
              <div style={{ height:44, background: change > 0 ? '#F0FDF4' : '#F8F9FA', border:'1px solid #E1E4E8', borderRadius:6, padding:'0 12px', display:'flex', alignItems:'center' }}>
                <span className="num" style={{ fontSize:16, fontWeight:700, color: change > 0 ? '#10B341' : '#BDC3C7' }}>
                  {change > 0 ? fmtT(change) : '—'}
                </span>
              </div>
            </div>
          </div>

          {/* Pay button — spec: full-width, 48px, navy, 18px bold */}
          <button
            onClick={() => cart.length > 0 && goPayment(total, cart)}
            disabled={cart.length === 0}
            style={{
              width:'100%', height:48, borderRadius:8, border:'none',
              background: cart.length === 0 ? '#E1E4E8' : '#10B341',
              color: cart.length === 0 ? '#7F8C8D' : '#fff',
              fontSize:18, fontWeight:700, fontFamily:'inherit',
              cursor: cart.length === 0 ? 'not-allowed' : 'pointer',
              display:'flex', alignItems:'center', justifyContent:'center', gap:10,
              transition:'background .15s',
            }}>
            <Icon name="cash" size={20}/> Pay & Complete Order
          </button>
        </div>
      </div>
    </div>
  );
};

// ── Payment screen — spec: centered modal/overlay, 560×540 white card ─────────
const PaymentScreen = ({ total = 3885, cart = EXAMPLE_CART, onComplete, onBack, initialTender = 'cash' }) => {
  const [tender,   setTender]   = React.useState(initialTender);
  const [received, setReceived] = React.useState(total);
  // expose tender setter globally for PPTX capture
  React.useEffect(() => { window.__setTender = setTender; return () => { delete window.__setTender; }; }, [setTender]);
  const change = Math.max(0, Number(received||0) - total);

  // Quick amount buttons per spec
  const quickAmts = [...new Set([total, Math.ceil(total/100)*100, Math.ceil(total/500)*500, Math.ceil(total/1000)*1000, 2000, 5000])].slice(0,5);

  // Tender button
  const TenderBtn = ({ id, icon, label }) => (
    <button onClick={() => setTender(id)} style={{
      height:60, padding:'0 12px',
      borderRadius:8, cursor:'pointer', fontFamily:'inherit',
      border: tender===id ? 'none' : '1px solid #E1E4E8',
      background: tender===id ? '#0F3A7D' : '#fff',
      color: tender===id ? '#fff' : '#2C3E50',
      display:'flex', flexDirection:'column', alignItems:'center', justifyContent:'center', gap:5,
      boxShadow: tender===id ? '0 4px 12px rgba(15,58,125,.25)' : '0 2px 4px rgba(0,0,0,.04)',
      transition:'all .12s',
    }}>
      <Icon name={icon} size={16}/>
      <span style={{ fontSize:13, fontWeight:600 }}>{label}</span>
    </button>
  );

  return (
    <div style={{ height:'100%', display:'flex', alignItems:'center', justifyContent:'center', background:'rgba(0,0,0,.18)', padding:32 }}>
      {/* spec: 560px wide, 540 tall approx, 20px padding, 12px radius */}
      <div style={{ background:'#fff', borderRadius:12, width:560, maxWidth:'96%', padding:28, boxShadow:'0 24px 56px rgba(0,0,0,.2)' }}>
        {/* Header */}
        <div style={{ textAlign:'center', marginBottom:20 }}>
          <h2 style={{ margin:'0 0 6px', fontSize:22, fontWeight:700, color:'#2C3E50' }}>Confirm Payment</h2>
          {/* Order summary rows */}
          <div style={{ display:'flex', flexDirection:'column', gap:3, margin:'14px 0', fontSize:14, color:'#7F8C8D' }}>
            <div style={{ display:'flex', justifyContent:'space-between' }}><span>Subtotal</span><span className="num">{fmtT(3800)}</span></div>
            <div style={{ display:'flex', justifyContent:'space-between' }}><span>Tax (5%)</span><span className="num">{fmtT(185)}</span></div>
          </div>
          {/* Total — spec: 44px bold navy, centered, largest */}
          <div className="num" style={{ fontSize:44, fontWeight:700, color:'#0F3A7D', letterSpacing:'-.02em' }}>
            PKR {total.toLocaleString()}
          </div>
        </div>

        {/* Tender buttons grid — spec: 2×2 */}
        <div style={{ display:'grid', gridTemplateColumns:'repeat(4,1fr)', gap:8, marginBottom:20 }}>
          <TenderBtn id="cash"   icon="cash"     label="Cash"/>
          <TenderBtn id="card"   icon="terminal" label="Card"/>
          <TenderBtn id="wallet" icon="qr"       label="Wallet"/>
          <TenderBtn id="split"  icon="filter"   label="Split"/>
        </div>

        {/* Cash section */}
        {tender === 'cash' && (
          <>
            <div style={{ marginBottom:12 }}>
              <div style={{ fontSize:13, color:'#7F8C8D', marginBottom:8, fontWeight:500 }}>Cash Received</div>
              <div style={{ display:'flex', alignItems:'center', height:52, background:'#F8F9FA', border:'1px solid #E1E4E8', borderRadius:8, padding:'0 14px', gap:8 }}>
                <span style={{ fontSize:13, color:'#7F8C8D' }}>PKR</span>
                <input className="num" value={received} onChange={e => setReceived(e.target.value.replace(/\D/g,''))}
                  style={{ flex:1, border:'none', outline:'none', background:'transparent', fontSize:24, fontFamily:'var(--mono)', fontWeight:700, color:'#2C3E50' }}/>
              </div>
            </div>

            {/* Quick amount buttons — spec: each 60px wide, 40px tall */}
            <div style={{ display:'flex', gap:8, marginBottom:14 }}>
              {quickAmts.map(q => (
                <button key={q} onClick={() => setReceived(q)} style={{
                  flex:1, height:40, borderRadius:6, border:'1px solid #E1E4E8',
                  background:'#fff', fontSize:13.5, fontFamily:'var(--mono)', fontWeight:600,
                  cursor:'pointer', color:'#2C3E50', transition:'background .08s',
                }}>
                  {q.toLocaleString()}
                </button>
              ))}
            </div>

            {/* Change display */}
            <div style={{ display:'grid', gridTemplateColumns:'1fr 1fr', gap:10, marginBottom:16 }}>
              <div style={{ padding:'12px 14px', borderRadius:8, background:'#F8F9FA' }}>
                <div style={{ fontSize:11, color:'#7F8C8D', textTransform:'uppercase', letterSpacing:'.05em', marginBottom:4 }}>Received</div>
                <div className="num" style={{ fontSize:20, fontWeight:700 }}>{fmtT(received)}</div>
              </div>
              <div style={{ padding:'12px 14px', borderRadius:8, background: change>=0?'#F0FDF4':'#FEF2F2' }}>
                <div style={{ fontSize:11, color: change>=0?'#0A7A2B':'#C0392B', textTransform:'uppercase', letterSpacing:'.05em', marginBottom:4 }}>Change</div>
                <div className="num" style={{ fontSize:20, fontWeight:700, color: change>=0?'#10B341':'#E74C3C' }}>{fmtT(change)}</div>
              </div>
            </div>
          </>
        )}

        {tender === 'card' && (
          <div style={{ padding:24, border:'2px dashed #E1E4E8', borderRadius:8, textAlign:'center', marginBottom:16 }}>
            <Icon name="terminal" size={36} color="#0F3A7D"/>
            <h3 style={{ margin:'12px 0 6px', fontSize:16, fontWeight:600, color:'#2C3E50' }}>Insert or tap card on terminal</h3>
            <p style={{ margin:0, fontSize:13, color:'#7F8C8D' }}>Ingenico iPP320 · Online authorization</p>
          </div>
        )}

        {/* Complete button — spec: full-width, 48px, navy */}
        <div style={{ display:'flex', flexDirection:'column', gap:8 }}>
          <button onClick={onComplete} style={{
            width:'100%', height:48, borderRadius:8, border:'none',
            background:'#0F3A7D', color:'#fff',
            fontSize:18, fontWeight:700, fontFamily:'inherit', cursor:'pointer',
            display:'flex', alignItems:'center', justifyContent:'center', gap:8,
            boxShadow:'0 4px 12px rgba(15,58,125,.25)',
          }}>
            <Icon name="check" size={18}/> Complete Payment
          </button>
          {/* Back link */}
          <button onClick={onBack} style={{ background:'none',border:'none',color:'#0F3A7D',fontSize:14,cursor:'pointer',fontFamily:'inherit',textDecoration:'underline',textUnderlineOffset:3 }}>
            Back to Cart
          </button>
        </div>
      </div>
    </div>
  );
};

// ── Receipt screen ────────────────────────────────────────────────────────────
const ReceiptScreen = ({ onNew }) => (
  <div style={{ display:'flex',alignItems:'center',justifyContent:'center',height:'100%',padding:28,background:'#F8F9FA',overflow:'auto' }}>
    <div style={{ display:'grid', gridTemplateColumns:'380px 320px', gap:28, alignItems:'flex-start' }}>
      {/* Thermal receipt paper */}
      <div style={{ background:'#fff', borderRadius:8, boxShadow:'var(--shadow-3)', padding:'26px 24px', fontFamily:'var(--mono)', fontSize:12.5, lineHeight:1.65 }}>
        <div style={{ textAlign:'center', marginBottom:12 }}>
          <div style={{ fontWeight:700, fontSize:14, letterSpacing:'.04em' }}>R TECHNOLOGIES POS</div>
          <div style={{ color:'#7F8C8D' }}>Main Branch · Karachi</div>
          <div style={{ color:'#7F8C8D' }}>NTN 4218992-1</div>
        </div>
        <div style={{ borderTop:'1px dashed #BDC3C7', borderBottom:'1px dashed #BDC3C7', padding:'8px 0', fontSize:11.5, marginBottom:10 }}>
          {[['Receipt #','MB-260520-00483'],['Date','20/05/2026 14:42'],['Cashier','Adeel (E1042)'],['Terminal','POS-01']].map(([k,v])=>(
            <div key={k} style={{ display:'flex', justifyContent:'space-between' }}><span>{k}</span><span>{v}</span></div>
          ))}
        </div>
        {EXAMPLE_CART.map(l => (
          <div key={l.id} style={{ marginBottom:5 }}>
            <div>{l.name}</div>
            <div style={{ display:'flex', justifyContent:'space-between', color:'#7F8C8D' }}>
              <span>  {l.qty} x {l.price.toLocaleString()}</span><span>{(l.qty*l.price).toLocaleString()}</span>
            </div>
          </div>
        ))}
        <div style={{ borderTop:'1px dashed #BDC3C7', padding:'8px 0', marginTop:4, fontSize:12 }}>
          {[['Subtotal','3,800'],['Tax 5%','185'],['Discount','-100']].map(([k,v])=>(
            <div key={k} style={{ display:'flex', justifyContent:'space-between' }}><span>{k}</span><span>{v}</span></div>
          ))}
        </div>
        <div style={{ borderTop:'1px dashed #BDC3C7', padding:'10px 0', fontSize:14, fontWeight:700 }}>
          <div style={{ display:'flex', justifyContent:'space-between' }}><span>TOTAL PKR</span><span>3,885</span></div>
          <div style={{ display:'flex', justifyContent:'space-between', fontWeight:400, fontSize:12 }}><span>Cash</span><span>4,000</span></div>
          <div style={{ display:'flex', justifyContent:'space-between', fontWeight:400, fontSize:12 }}><span>Change</span><span>115</span></div>
        </div>
        <div style={{ textAlign:'center', fontSize:11, color:'#7F8C8D', marginTop:10 }}>FBR Invoice ID: 26050000019874320</div>
      </div>

      {/* Actions */}
      <div>
        <div style={{ width:52,height:52,borderRadius:'50%',background:'#F0FDF4',display:'grid',placeItems:'center',marginBottom:14 }}>
          <Icon name="check" size={26} color="#10B341"/>
        </div>
        <div style={{ fontSize:11,color:'#7F8C8D',letterSpacing:'.06em',textTransform:'uppercase' }}>Order complete</div>
        <h1 style={{ margin:'6px 0 4px',fontSize:26,fontWeight:700,letterSpacing:'-.02em',color:'#2C3E50' }}>Paid {fmtT(3885)}</h1>
        <div style={{ fontSize:13.5,color:'#7F8C8D',marginBottom:20 }}>Change given <span className="num" style={{ color:'#10B341',fontWeight:700 }}>PKR 115</span></div>
        <div style={{ display:'flex',flexDirection:'column',gap:10 }}>
          <Btn variant="primary" size="lg" icon="print" full>Reprint receipt</Btn>
          <Btn variant="default" size="lg" icon="receipt" full>Email / SMS receipt</Btn>
          <button onClick={onNew} style={{ width:'100%',height:52,borderRadius:8,border:'none',background:'#10B341',color:'#fff',fontSize:16,fontWeight:700,fontFamily:'inherit',cursor:'pointer',display:'flex',alignItems:'center',justifyContent:'center',gap:9,marginTop:4,boxShadow:'0 4px 12px rgba(16,179,65,.25)' }}>
            <Icon name="plus" size={18}/> New Order
          </button>
        </div>
        <div style={{ marginTop:14,padding:10,borderRadius:8,background:'#fff',border:'1px solid #E1E4E8',fontSize:12,display:'flex',justifyContent:'space-between',color:'#7F8C8D' }}>
          <span>Sync status</span>
          <span style={{ color:'#10B341',display:'inline-flex',alignItems:'center',gap:5 }}>
            <Dot tone="success" size={5}/> Synced
          </span>
        </div>
      </div>
    </div>
  </div>
);

Object.assign(window, {
  CheckoutScreen, PaymentScreen, ReceiptScreen, ManagerApprovalModal, VoidItemModal,
});
