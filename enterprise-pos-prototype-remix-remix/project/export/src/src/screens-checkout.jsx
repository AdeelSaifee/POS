// Checkout: POS, search, grid, cart, payment, receipt, manager approval modal, void modal

// ----- Item tile -----
const ItemTile = ({ item, onAdd }) => (
  <button onClick={() => onAdd(item)} style={{
    background: '#fff', border: '1px solid var(--line)', borderRadius: 10,
    padding: 10, cursor: 'pointer', textAlign: 'left',
    display: 'flex', flexDirection: 'column', gap: 8,
    transition: 'border-color .12s, transform .04s',
    position: 'relative', overflow: 'hidden',
  }}
  onMouseEnter={(e) => e.currentTarget.style.borderColor = 'var(--navy-800)'}
  onMouseLeave={(e) => e.currentTarget.style.borderColor = 'var(--line)'}
  >
    <Placeholder label={item.sku} ratio="4/3"/>
    {item.stock < 20 && (
      <span style={{ position: 'absolute', top: 8, right: 8, padding: '2px 6px', borderRadius: 4, background: 'var(--amber-50)', color: 'var(--amber-700)', fontSize: 10, fontWeight: 500 }}>
        {item.stock} left
      </span>
    )}
    <div>
      <div style={{ fontSize: 13, fontWeight: 500, color: 'var(--ink-900)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{item.name}</div>
      <div className="num" style={{ fontSize: 11, color: 'var(--ink-400)' }}>{item.sku}</div>
    </div>
    <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between' }}>
      <span className="num" style={{ fontSize: 15, fontWeight: 600 }}>{fmtT(item.price)}</span>
      <span style={{ fontSize: 11, color: 'var(--ink-500)' }}>per {item.unit}</span>
    </div>
  </button>
);

// ----- Cart line -----
const CartLine = ({ line, onQty, onRemove, selected, onSelect }) => (
  <div onClick={onSelect} style={{
    padding: '12px 14px', display: 'grid', gridTemplateColumns: '40px 1fr auto', gap: 12, alignItems: 'center',
    borderBottom: '1px solid var(--line-soft)', cursor: 'pointer',
    background: selected ? 'var(--blue-50)' : 'transparent',
    borderLeft: selected ? '3px solid var(--blue-600)' : '3px solid transparent',
  }}>
    <Placeholder label="" ratio="1/1" style={{ height: 40, width: 40, borderRadius: 6 }}/>
    <div style={{ minWidth: 0 }}>
      <div style={{ fontSize: 13.5, fontWeight: 500, color: 'var(--ink-900)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{line.name}</div>
      <div className="num" style={{ fontSize: 11.5, color: 'var(--ink-500)', marginTop: 2 }}>
        {line.qty} × {fmtT(line.price)} <span style={{ color: 'var(--ink-400)' }}>/ {line.unit}</span>
      </div>
    </div>
    <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
      <button onClick={(e) => { e.stopPropagation(); onQty(line.id, -1); }} style={{
        width: 28, height: 28, borderRadius: 6, border: '1px solid var(--line)', background: '#fff', cursor: 'pointer',
      }}><Icon name="minus" size={12}/></button>
      <span className="num" style={{ fontSize: 14, fontWeight: 500, minWidth: 22, textAlign: 'center' }}>{line.qty}</span>
      <button onClick={(e) => { e.stopPropagation(); onQty(line.id, 1); }} style={{
        width: 28, height: 28, borderRadius: 6, border: '1px solid var(--line)', background: '#fff', cursor: 'pointer',
      }}><Icon name="plus" size={12}/></button>
      <div className="num" style={{ minWidth: 80, textAlign: 'right', fontSize: 14, fontWeight: 600 }}>
        {fmtT(line.qty * line.price)}
      </div>
    </div>
  </div>
);

// ----- Manager Approval Modal -----
const ManagerApprovalModal = ({ open, action, onApprove, onReject, onClose }) => {
  const [mgrId, setMgrId] = React.useState("E1043");
  const [pin, setPin] = React.useState("");
  const [comment, setComment] = React.useState("");
  const [reason, setReason] = React.useState(REASON_CODES[0]);

  return (
    <Modal open={open} onClose={onClose} width={620}>
      <ModalHeader
        title="Manager approval required"
        subtitle={action || "Protected action — manager identity will be recorded on the audit ledger."}
        onClose={onClose}
        tone="amber"
      />
      <div style={{ padding: 22, display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 22 }}>
        <div>
          <Field label="Reason code" style={{ marginBottom: 14 }}>
            <Select full value={reason} onChange={setReason} options={REASON_CODES}/>
          </Field>
          <Field label="Operator comment" style={{ marginBottom: 14 }}>
            <textarea value={comment} onChange={(e) => setComment(e.target.value)} placeholder="Add a short justification..."
              style={{ width: '100%', minHeight: 70, border: '1px solid var(--line-hard)', borderRadius: 8, padding: 10, fontSize: 13, fontFamily: 'inherit', resize: 'vertical' }}/>
          </Field>
          <div style={{ padding: 12, background: 'var(--surface-2)', borderRadius: 8, fontSize: 12.5, color: 'var(--ink-600)', lineHeight: 1.5 }}>
            <div style={{ fontWeight: 500, color: 'var(--ink-800)', marginBottom: 4 }}>Audit trail</div>
            <div>Operator · <span className="num">E1042 Adeel</span></div>
            <div>Terminal · <span className="num">POS-01</span></div>
            <div>Business date · <span className="num">19 May 2026 14:42</span></div>
            <div>Correlation · <span className="num">crl-7f3a-8b22</span></div>
          </div>
        </div>
        <div>
          <Field label="Manager" style={{ marginBottom: 14 }}>
            <Select full value={mgrId} onChange={setMgrId} options={[
              { value: "E1043", label: "Fahad — Manager · Main Branch" },
              { value: "E1046", label: "Hira — Manager · City Branch (remote)" },
            ]}/>
          </Field>
          <Field label="Manager PIN" style={{ marginBottom: 12 }}>
            <div style={{ display: 'flex', justifyContent: 'center', gap: 8, marginBottom: 12 }}>
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} style={{
                  width: 34, height: 40, borderRadius: 8,
                  border: '1px solid ' + (i < pin.length ? 'var(--navy-800)' : 'var(--line-hard)'),
                  background: i < pin.length ? 'var(--surface-2)' : '#fff',
                  display: 'grid', placeItems: 'center', fontSize: 18, fontWeight: 600,
                }}>{i < pin.length ? "•" : ""}</div>
              ))}
            </div>
            <PinPad onKey={(k) => {
              if (k === "C") setPin("");
              else if (k === "⌫") setPin(p => p.slice(0, -1));
              else if (pin.length < 4) setPin(p => p + k);
            }}/>
          </Field>
        </div>
      </div>
      <div style={{ padding: 18, borderTop: '1px solid var(--line)', display: 'flex', justifyContent: 'space-between', gap: 10, background: 'var(--surface)' }}>
        <Btn variant="ghost" onClick={onClose}>Cancel</Btn>
        <div style={{ display: 'flex', gap: 10 }}>
          <Btn variant="dangerOutline" onClick={onReject}>Reject</Btn>
          <Btn variant="success" icon="check" disabled={pin.length < 4} onClick={onApprove}>Approve action</Btn>
        </div>
      </div>
    </Modal>
  );
};

// ----- Void / Remove Item Modal -----
const VoidItemModal = ({ open, line, onConfirm, onClose, onRequestManager }) => {
  const [reason, setReason] = React.useState(REASON_CODES[0]);
  const [comment, setComment] = React.useState("");
  if (!line) return null;
  return (
    <Modal open={open} onClose={onClose} width={540}>
      <ModalHeader title="Void item from order" subtitle="This is an append-only event — the original line stays in the ledger." onClose={onClose} tone="danger"/>
      <div style={{ padding: 22 }}>
        <div style={{ display: 'flex', gap: 14, alignItems: 'center', padding: 14, background: 'var(--surface-2)', borderRadius: 10, marginBottom: 18 }}>
          <Placeholder label="" ratio="1/1" style={{ height: 56, width: 56 }}/>
          <div style={{ flex: 1 }}>
            <div style={{ fontWeight: 500, fontSize: 14 }}>{line.name}</div>
            <div className="num" style={{ fontSize: 12.5, color: 'var(--ink-500)' }}>{line.qty} × {fmtT(line.price)} = {fmtT(line.qty * line.price)}</div>
          </div>
          <Badge tone="danger" dot>Void</Badge>
        </div>
        <Field label="Reason code" style={{ marginBottom: 12 }}>
          <Select full value={reason} onChange={setReason} options={REASON_CODES}/>
        </Field>
        <Field label="Comment">
          <textarea value={comment} onChange={(e) => setComment(e.target.value)} placeholder="Optional..."
            style={{ width: '100%', minHeight: 70, border: '1px solid var(--line-hard)', borderRadius: 8, padding: 10, fontSize: 13, fontFamily: 'inherit', resize: 'vertical' }}/>
        </Field>
        <div style={{ marginTop: 14, padding: 12, background: 'var(--amber-50)', borderRadius: 8, fontSize: 12.5, color: 'var(--amber-700)', display: 'flex', gap: 10 }}>
          <Icon name="warning" size={16} color="var(--amber-700)"/>
          <span>Voids of lines above PKR 1,000 require manager approval.</span>
        </div>
      </div>
      <div style={{ padding: 16, borderTop: '1px solid var(--line)', display: 'flex', justifyContent: 'flex-end', gap: 10, background: 'var(--surface)' }}>
        <Btn variant="ghost" onClick={onClose}>Cancel</Btn>
        <Btn variant="amber" icon="user" onClick={onRequestManager}>Request manager</Btn>
        <Btn variant="danger" icon="trash" onClick={onConfirm}>Void item</Btn>
      </div>
    </Modal>
  );
};

// ----- Main POS Checkout -----
const CheckoutScreen = ({ goPayment, openManager, openVoid, statusBarHeight, sidebarWidth }) => {
  const [view, setView] = React.useState("grid"); // grid | search
  const [cat, setCat] = React.useState("all");
  const [query, setQuery] = React.useState("");
  const [cart, setCart] = React.useState(EXAMPLE_CART.map(l => ({ ...l })));
  const [selected, setSelected] = React.useState(null);
  const [customer, setCustomer] = React.useState(null);

  const filteredItems = ITEMS.filter(i =>
    (cat === "all" || i.cat === cat) &&
    (query === "" || i.name.toLowerCase().includes(query.toLowerCase()) || i.sku.toLowerCase().includes(query.toLowerCase()))
  );

  const addItem = (item) => {
    setCart(c => {
      const ex = c.find(l => l.id === item.id);
      if (ex) return c.map(l => l.id === item.id ? { ...l, qty: l.qty + 1 } : l);
      return [...c, { id: item.id, name: item.name, qty: 1, unit: item.unit, price: item.price }];
    });
  };
  const changeQty = (id, delta) => {
    setCart(c => c.map(l => l.id === id ? { ...l, qty: Math.max(1, l.qty + delta) } : l));
  };
  const removeLine = (id) => setCart(c => c.filter(l => l.id !== id));

  const subtotal = cart.reduce((s, l) => s + l.qty * l.price, 0);
  const discount = 100;
  const tax = Math.round((subtotal - discount) * 0.05);
  const total = subtotal - discount + tax;

  return (
    <div style={{ display: 'grid', gridTemplateColumns: '1fr 440px', height: '100%', overflow: 'hidden' }}>
      {/* LEFT — Catalog / Search */}
      <div style={{ display: 'flex', flexDirection: 'column', minWidth: 0, borderRight: '1px solid var(--line)' }}>
        {/* Search bar */}
        <div style={{ padding: '14px 20px', background: '#fff', borderBottom: '1px solid var(--line)', display: 'flex', alignItems: 'center', gap: 10 }}>
          <div style={{ flex: 1, display: 'flex', alignItems: 'center', gap: 10, padding: '0 14px', height: 44, background: 'var(--surface-2)', borderRadius: 10 }}>
            <Icon name="scan" size={18} color="var(--ink-500)"/>
            <input value={query} onChange={(e) => { setQuery(e.target.value); setView(e.target.value ? "search" : "grid"); }}
              placeholder="Scan barcode or search by name, SKU, identifier..."
              style={{ flex: 1, background: 'transparent', border: 'none', outline: 'none', fontSize: 14.5, fontFamily: 'inherit' }}/>
            <kbd style={{ background: '#fff', border: '1px solid var(--line)', borderRadius: 4, padding: '1px 6px', fontFamily: 'var(--mono)', fontSize: 11, color: 'var(--ink-500)' }}>F2</kbd>
          </div>
          <button onClick={() => setView(v => v === 'grid' ? 'search' : 'grid')} style={{
            height: 44, padding: '0 12px', border: '1px solid var(--line)', background: '#fff', borderRadius: 10, cursor: 'pointer',
            display: 'inline-flex', alignItems: 'center', gap: 6, color: 'var(--ink-700)',
          }}>
            <Icon name={view === 'grid' ? "list" : "grid"} size={16}/>
            {view === 'grid' ? "List view" : "Grid view"}
          </button>
          <Btn variant="default" icon="qr">QR lookup</Btn>
        </div>

        {/* Categories */}
        <div style={{ padding: '12px 20px', background: '#fff', borderBottom: '1px solid var(--line)', display: 'flex', gap: 6, overflowX: 'auto' }}>
          {CATEGORIES.map(c => (
            <button key={c.id} onClick={() => setCat(c.id)} style={{
              padding: '8px 14px', borderRadius: 999, cursor: 'pointer', whiteSpace: 'nowrap',
              border: '1px solid ' + (cat === c.id ? 'var(--navy-800)' : 'var(--line)'),
              background: cat === c.id ? 'var(--navy-800)' : '#fff',
              color: cat === c.id ? '#fff' : 'var(--ink-700)',
              fontSize: 13, fontWeight: 500,
              display: 'inline-flex', alignItems: 'center', gap: 8,
            }}>
              {c.name}
              <span style={{
                fontSize: 11, padding: '0 6px', borderRadius: 99,
                background: cat === c.id ? 'rgba(255,255,255,.15)' : 'var(--surface-2)',
                color: cat === c.id ? 'rgba(255,255,255,.85)' : 'var(--ink-500)',
                fontFamily: 'var(--mono)',
              }}>{c.count}</span>
            </button>
          ))}
        </div>

        {/* Grid / Search results */}
        <div style={{ flex: 1, overflow: 'auto', padding: 20, background: 'var(--surface)' }}>
          {view === 'search' && query && (
            <div style={{ marginBottom: 10, fontSize: 12.5, color: 'var(--ink-500)' }}>
              {filteredItems.length} result{filteredItems.length === 1 ? '' : 's'} for "<span style={{ color: 'var(--ink-800)' }}>{query}</span>"
            </div>
          )}
          {view === 'grid' || view === 'search' ? (
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))', gap: 12 }}>
              {filteredItems.map(item => <ItemTile key={item.id} item={item} onAdd={addItem}/>)}
            </div>
          ) : null}
        </div>
      </div>

      {/* RIGHT — Cart */}
      <div style={{ display: 'flex', flexDirection: 'column', background: '#fff', minWidth: 0 }}>
        {/* Order header */}
        <div style={{ padding: '16px 20px', borderBottom: '1px solid var(--line)' }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 4 }}>
            <div>
              <div style={{ fontSize: 11.5, color: 'var(--ink-400)', letterSpacing: '.06em', textTransform: 'uppercase', fontWeight: 500 }}>Active order</div>
              <div className="num" style={{ fontSize: 15, fontWeight: 600, marginTop: 2 }}>ORD-MB-20260519-00483</div>
            </div>
            <Badge tone="blue" dot>Open</Badge>
          </div>
          <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
            {customer ? (
              <div style={{ flex: 1, padding: '8px 12px', background: 'var(--blue-50)', borderRadius: 8, display: 'flex', alignItems: 'center', gap: 8, fontSize: 13 }}>
                <Icon name="user" size={14} color="var(--blue-600)"/>
                <span style={{ flex: 1, fontWeight: 500 }}>{customer}</span>
                <button onClick={() => setCustomer(null)} style={{ background: 'transparent', border: 'none', cursor: 'pointer', color: 'var(--ink-500)' }}>
                  <Icon name="close" size={14}/>
                </button>
              </div>
            ) : (
              <button onClick={() => setCustomer("Ahmed Khan · +92 300 1234567")} style={{
                flex: 1, padding: '8px 12px', background: 'var(--surface-2)', border: '1px dashed var(--line-hard)', borderRadius: 8,
                color: 'var(--ink-500)', cursor: 'pointer', fontSize: 13, display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: 6,
              }}>
                <Icon name="plus" size={14}/> Attach customer
              </button>
            )}
            <button style={{ padding: '8px 12px', background: 'var(--surface-2)', border: '1px solid var(--line)', borderRadius: 8, color: 'var(--ink-700)', cursor: 'pointer', fontSize: 13 }}>
              Hold
            </button>
          </div>
        </div>

        {/* Cart lines */}
        <div style={{ flex: 1, overflow: 'auto' }}>
          {cart.length === 0 && (
            <div style={{ padding: 60, textAlign: 'center', color: 'var(--ink-400)' }}>
              <Icon name="cart" size={32} color="var(--ink-300)"/>
              <div style={{ marginTop: 10, fontSize: 13 }}>Scan or tap an item to start.</div>
            </div>
          )}
          {cart.map(l => (
            <CartLine key={l.id} line={l} onQty={changeQty}
              selected={selected === l.id}
              onSelect={() => setSelected(s => s === l.id ? null : l.id)}
              onRemove={() => removeLine(l.id)}/>
          ))}
        </div>

        {/* Line actions */}
        {selected && (
          <div style={{ padding: '10px 16px', background: 'var(--surface-2)', borderTop: '1px solid var(--line)', display: 'flex', gap: 8 }}>
            <Btn variant="default" size="sm" icon="edit">Edit price</Btn>
            <Btn variant="default" size="sm" icon="minus">Discount</Btn>
            <div style={{ flex: 1 }}/>
            <Btn variant="dangerOutline" size="sm" icon="trash" onClick={() => openVoid(cart.find(l => l.id === selected))}>Void line</Btn>
          </div>
        )}

        {/* Totals */}
        <div style={{ padding: '14px 20px', borderTop: '1px solid var(--line)', background: 'var(--surface)' }}>
          <KV k="Subtotal" v={fmtT(subtotal)} mono/>
          <KV k="Discount" v={`− ${fmtT(discount)}`} mono color="var(--red-700)"/>
          <KV k="Tax (5%)" v={fmtT(tax)} mono/>
          <div style={{
            display: 'flex', justifyContent: 'space-between', alignItems: 'baseline',
            padding: '14px 0 0', marginTop: 6, borderTop: '1px solid var(--line)',
          }}>
            <span style={{ fontSize: 14, fontWeight: 500, color: 'var(--ink-700)' }}>Total due</span>
            <span className="num" style={{ fontSize: 36, fontWeight: 600, letterSpacing: '-.02em', color: 'var(--navy-800)' }}>{fmtT(total)}</span>
          </div>
        </div>

        {/* Pay */}
        <div style={{ padding: 16, borderTop: '1px solid var(--line)' }}>
          <Btn variant="success" size="xl" full disabled={cart.length === 0} onClick={() => goPayment(total, cart)}>
            <Icon name="cash" size={22}/> Charge {fmtT(total)} <Icon name="arrowR" size={18}/>
          </Btn>
        </div>
      </div>
    </div>
  );
};

// ----- Payment Screen -----
const PaymentScreen = ({ total = 3885, cart = EXAMPLE_CART, onComplete, onBack }) => {
  const [tender, setTender] = React.useState("cash");
  const [received, setReceived] = React.useState(total);
  const change = Math.max(0, Number(received || 0) - total);

  const quickCash = [total, Math.ceil(total / 100) * 100, Math.ceil(total / 500) * 500, Math.ceil(total / 1000) * 1000, 5000, 10000]
    .filter((v, i, a) => a.indexOf(v) === i).slice(0, 6);

  return (
    <div style={{ display: 'grid', gridTemplateColumns: '1fr 480px', height: '100%' }}>
      <div style={{ padding: 32, overflow: 'auto' }}>
        <button onClick={onBack} style={{ background: 'transparent', border: 'none', cursor: 'pointer', color: 'var(--ink-500)', fontSize: 13, display: 'inline-flex', alignItems: 'center', gap: 6, marginBottom: 16 }}>
          <Icon name="chevronL" size={16}/> Back to cart
        </button>

        <div style={{ marginBottom: 24 }}>
          <div style={{ fontSize: 11.5, color: 'var(--ink-400)', letterSpacing: '.08em', textTransform: 'uppercase' }}>Take payment</div>
          <h1 style={{ margin: '4px 0 0', fontSize: 28, fontWeight: 600, letterSpacing: '-.02em' }}>Total due {fmtT(total)}</h1>
        </div>

        {/* Tender selector */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 10, marginBottom: 24 }}>
          {[
            { id: "cash", icon: "cash", label: "Cash" },
            { id: "card", icon: "terminal", label: "Card / EMV" },
            { id: "wallet", icon: "qr", label: "Wallet" },
            { id: "split", icon: "filter", label: "Split" },
          ].map(t => (
            <button key={t.id} onClick={() => setTender(t.id)} style={{
              padding: 18, borderRadius: 10, cursor: 'pointer',
              border: '1px solid ' + (tender === t.id ? 'var(--navy-800)' : 'var(--line)'),
              background: tender === t.id ? 'var(--navy-800)' : '#fff',
              color: tender === t.id ? '#fff' : 'var(--ink-800)',
              display: 'flex', flexDirection: 'column', alignItems: 'flex-start', gap: 8,
            }}>
              <Icon name={t.icon} size={20}/>
              <span style={{ fontSize: 14, fontWeight: 500 }}>{t.label}</span>
            </button>
          ))}
        </div>

        {tender === "cash" && (
          <Card padding={22}>
            <SectionTitle>Cash received</SectionTitle>
            <Input full style={{ height: 64, fontSize: 28 }} value={received} onChange={(e) => setReceived(e.target.value.replace(/\D/g,''))}/>
            <div style={{ marginTop: 12, display: 'grid', gridTemplateColumns: 'repeat(6, 1fr)', gap: 8 }}>
              {quickCash.map(q => (
                <button key={q} onClick={() => setReceived(q)} style={{
                  padding: '12px 0', borderRadius: 8, border: '1px solid var(--line)', background: '#fff',
                  fontSize: 14, fontFamily: 'var(--mono)', fontWeight: 500, cursor: 'pointer', color: 'var(--ink-800)',
                }}>{q.toLocaleString()}</button>
              ))}
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginTop: 18 }}>
              <div style={{ padding: 16, borderRadius: 10, background: 'var(--surface-2)' }}>
                <div style={{ fontSize: 11.5, color: 'var(--ink-500)', textTransform: 'uppercase', letterSpacing: '.05em' }}>Received</div>
                <div className="num" style={{ fontSize: 28, fontWeight: 600, marginTop: 4 }}>{fmtT(received)}</div>
              </div>
              <div style={{ padding: 16, borderRadius: 10, background: change >= 0 ? 'var(--green-50)' : 'var(--red-50)' }}>
                <div style={{ fontSize: 11.5, color: change >= 0 ? 'var(--green-700)' : 'var(--red-700)', textTransform: 'uppercase', letterSpacing: '.05em' }}>Change due</div>
                <div className="num" style={{ fontSize: 28, fontWeight: 600, marginTop: 4, color: change >= 0 ? 'var(--green-700)' : 'var(--red-700)' }}>{fmtT(change)}</div>
              </div>
            </div>
          </Card>
        )}

        {tender === "card" && (
          <Card padding={22} style={{ textAlign: 'center' }}>
            <div style={{ padding: 32, border: '2px dashed var(--line-hard)', borderRadius: 12 }}>
              <Icon name="terminal" size={40} color="var(--blue-600)"/>
              <h3 style={{ margin: '14px 0 6px', fontSize: 18 }}>Awaiting card on terminal</h3>
              <p style={{ margin: 0, color: 'var(--ink-500)', fontSize: 13 }}>Ingenico iPP320 · Online authorization</p>
              <div style={{ marginTop: 18, display: 'inline-flex', alignItems: 'center', gap: 8, padding: '8px 14px', background: 'var(--blue-50)', borderRadius: 99, fontSize: 12.5, color: 'var(--blue-600)' }}>
                <Dot tone="blue"/> Insert, tap, or swipe to authorize {fmtT(total)}
              </div>
            </div>
          </Card>
        )}

        {tender === "wallet" && (
          <Card padding={22} style={{ textAlign: 'center' }}>
            <div style={{ display: 'inline-block', padding: 16, background: '#fff', borderRadius: 12, border: '1px solid var(--line)' }}>
              <svg width="140" height="140" viewBox="0 0 21 21">
                <rect width="21" height="21" fill="#fff"/>
                {Array.from({ length: 441 }).map((_, i) => {
                  const x = i % 21, y = Math.floor(i / 21);
                  const fill = (x*y*7 + x + y*3) % 5 < 2 || (x < 7 && y < 7) || (x > 13 && y < 7) || (x < 7 && y > 13);
                  return fill ? <rect key={i} x={x} y={y} width="1" height="1" fill="#0F2C5C"/> : null;
                })}
              </svg>
            </div>
            <h3 style={{ margin: '14px 0 4px', fontSize: 16 }}>Scan to pay {fmtT(total)}</h3>
            <p style={{ margin: 0, color: 'var(--ink-500)', fontSize: 12.5 }}>JazzCash · Easypaisa · NayaPay</p>
          </Card>
        )}

        {tender === "split" && (
          <Card padding={22}>
            <SectionTitle>Split tender</SectionTitle>
            {[
              { id: "1", method: "Cash", amt: 2000 },
              { id: "2", method: "Card", amt: 1885 },
            ].map(s => (
              <div key={s.id} style={{ display: 'grid', gridTemplateColumns: '160px 1fr 32px', gap: 10, alignItems: 'center', padding: '10px 0', borderBottom: '1px solid var(--line-soft)' }}>
                <Select full value={s.method} options={["Cash","Card / EMV","Wallet","Voucher"]} onChange={()=>{}}/>
                <Input full value={s.amt} onChange={()=>{}}/>
                <button style={{ width: 32, height: 32, border: 'none', background: 'transparent', cursor: 'pointer', color: 'var(--red-600)' }}>
                  <Icon name="trash" size={16}/>
                </button>
              </div>
            ))}
            <Btn variant="default" size="sm" icon="plus" style={{ marginTop: 12 }}>Add tender</Btn>
            <div style={{ marginTop: 18, padding: 14, background: 'var(--green-50)', borderRadius: 10, display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--green-700)', fontWeight: 500 }}>Split balances</span>
              <span className="num" style={{ color: 'var(--green-700)', fontWeight: 600 }}>{fmtT(total)} / {fmtT(total)}</span>
            </div>
          </Card>
        )}
      </div>

      {/* Right: order summary */}
      <div style={{ background: '#fff', borderLeft: '1px solid var(--line)', display: 'flex', flexDirection: 'column' }}>
        <div style={{ padding: '20px 22px', borderBottom: '1px solid var(--line)' }}>
          <div style={{ fontSize: 11.5, color: 'var(--ink-400)', textTransform: 'uppercase', letterSpacing: '.05em' }}>Order summary</div>
          <div className="num" style={{ fontSize: 14, color: 'var(--ink-700)', marginTop: 4 }}>ORD-MB-20260519-00483</div>
        </div>
        <div style={{ flex: 1, overflow: 'auto', padding: '8px 0' }}>
          {cart.map(l => (
            <div key={l.id} style={{ padding: '10px 22px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: 13 }}>
              <div>
                <div style={{ fontWeight: 500 }}>{l.name}</div>
                <div className="num" style={{ fontSize: 11.5, color: 'var(--ink-500)', marginTop: 2 }}>{l.qty} × {fmtT(l.price)}</div>
              </div>
              <span className="num" style={{ fontWeight: 600 }}>{fmtT(l.qty * l.price)}</span>
            </div>
          ))}
        </div>
        <div style={{ padding: '16px 22px', borderTop: '1px solid var(--line)', background: 'var(--surface)' }}>
          <KV k="Subtotal" v={fmtT(3800)} mono/>
          <KV k="Discount" v={`− ${fmtT(100)}`} mono color="var(--red-700)"/>
          <KV k="Tax" v={fmtT(185)} mono/>
          <KV k="Total" v={fmtT(total)} mono big strong divider/>
        </div>
        <div style={{ padding: 16, borderTop: '1px solid var(--line)' }}>
          <Btn variant="success" size="xl" full onClick={onComplete}>
            <Icon name="check" size={20}/> Complete order
          </Btn>
        </div>
      </div>
    </div>
  );
};

// ----- Receipt -----
const ReceiptScreen = ({ onNew }) => (
  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', padding: 32, background: 'var(--surface)', overflow: 'auto' }}>
    <div style={{ display: 'grid', gridTemplateColumns: '420px 360px', gap: 32, alignItems: 'flex-start' }}>
      {/* Receipt paper */}
      <div style={{
        background: '#fff', borderRadius: 8, boxShadow: 'var(--shadow-3)',
        padding: '32px 28px', fontFamily: 'var(--mono)', fontSize: 12.5, lineHeight: 1.6,
        position: 'relative',
      }}>
        <div style={{ position: 'absolute', top: -8, left: 0, right: 0, height: 8,
          background: 'radial-gradient(circle, transparent 60%, #fff 60%)',
          backgroundSize: '12px 12px', backgroundPosition: 'center' }}/>
        <div style={{ textAlign: 'center', marginBottom: 14 }}>
          <div style={{ fontWeight: 600, fontSize: 14, letterSpacing: '.05em' }}>ENTERPRISE RETAIL CO.</div>
          <div style={{ color: 'var(--ink-500)' }}>Main Branch · Karachi</div>
          <div style={{ color: 'var(--ink-500)' }}>NTN 4218992-1 · STN 17-77-7720</div>
        </div>
        <div style={{ borderTop: '1px dashed var(--ink-300)', borderBottom: '1px dashed var(--ink-300)', padding: '8px 0', fontSize: 11.5 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>Receipt #</span><span>MB-260519-00483</span></div>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>Date</span><span>19/05/2026 14:42</span></div>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>Cashier</span><span>Adeel (E1042)</span></div>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>Terminal</span><span>POS-01</span></div>
        </div>
        <div style={{ padding: '10px 0' }}>
          {EXAMPLE_CART.map(l => (
            <div key={l.id} style={{ marginBottom: 4 }}>
              <div>{l.name}</div>
              <div style={{ display: 'flex', justifyContent: 'space-between', color: 'var(--ink-500)' }}>
                <span>  {l.qty} x {l.price.toLocaleString()}</span>
                <span>{(l.qty * l.price).toLocaleString()}</span>
              </div>
            </div>
          ))}
        </div>
        <div style={{ borderTop: '1px dashed var(--ink-300)', padding: '8px 0', fontSize: 12 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>Subtotal</span><span>3,800</span></div>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>Discount</span><span>-100</span></div>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>Tax 5%</span><span>185</span></div>
        </div>
        <div style={{ borderTop: '1px dashed var(--ink-300)', padding: '10px 0', fontSize: 14, fontWeight: 600 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>TOTAL PKR</span><span>3,885</span></div>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontWeight: 400, fontSize: 12 }}><span>Cash</span><span>4,000</span></div>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontWeight: 400, fontSize: 12 }}><span>Change</span><span>115</span></div>
        </div>
        <div style={{ textAlign: 'center', padding: '12px 0 0', color: 'var(--ink-500)' }}>
          <div>Thank you for shopping with us</div>
          <div style={{ fontSize: 10, marginTop: 8 }}>FBR Invoice ID: 26050000019874320</div>
        </div>
        <div style={{ marginTop: 14, display: 'flex', justifyContent: 'center' }}>
          {/* Barcode-ish */}
          <svg width="200" height="38" viewBox="0 0 200 38">
            {Array.from({ length: 50 }).map((_, i) => (
              <rect key={i} x={i*4} y={0} width={(i*13)%5===0?3:1} height={32} fill="#0B1220"/>
            ))}
            <text x="100" y="37" textAnchor="middle" fontSize="6" fontFamily="monospace" fill="#0B1220">MB-260519-00483</text>
          </svg>
        </div>
      </div>

      {/* Actions */}
      <div>
        <div style={{ marginBottom: 18 }}>
          <div style={{
            width: 56, height: 56, borderRadius: 99, background: 'var(--green-50)',
            display: 'grid', placeItems: 'center', marginBottom: 14,
          }}>
            <Icon name="check" size={28} color="var(--green-600)"/>
          </div>
          <div style={{ fontSize: 11.5, color: 'var(--ink-400)', letterSpacing: '.08em', textTransform: 'uppercase' }}>Order complete</div>
          <h1 style={{ margin: '6px 0 4px', fontSize: 28, fontWeight: 600, letterSpacing: '-.02em' }}>Paid {fmtT(3885)}</h1>
          <div style={{ fontSize: 13.5, color: 'var(--ink-500)' }}>Change given <span className="num" style={{ color: 'var(--green-700)', fontWeight: 500 }}>PKR 115</span></div>
        </div>

        <Card padding={16} style={{ marginBottom: 14, background: 'var(--green-50)', borderColor: 'var(--green-100)' }}>
          <div style={{ display: 'flex', gap: 10 }}>
            <Icon name="check" size={18} color="var(--green-700)"/>
            <div style={{ fontSize: 12.5, color: 'var(--green-700)' }}>
              Printed on Epson TM-T88VII · Cash drawer kicked.
            </div>
          </div>
        </Card>

        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          <Btn variant="primary" size="lg" icon="print" full>Reprint receipt</Btn>
          <Btn variant="default" size="lg" icon="receipt" full>Email / SMS receipt</Btn>
          <Btn variant="success" size="xl" full onClick={onNew}>
            <Icon name="plus" size={20}/> New order
          </Btn>
        </div>

        <div style={{ marginTop: 16, padding: 12, borderRadius: 8, background: '#fff', border: '1px solid var(--line)', fontSize: 12 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', color: 'var(--ink-500)' }}>
            <span>Sync status</span>
            <span style={{ color: 'var(--green-700)', display: 'inline-flex', alignItems: 'center', gap: 6 }}>
              <Dot tone="success" size={6}/> Queued · will sync in ≤2s
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
);

Object.assign(window, {
  CheckoutScreen, PaymentScreen, ReceiptScreen, ManagerApprovalModal, VoidItemModal,
});
