// Cash screens: Drawer, Drawer→Vault, Vault→Bank

const CashDrawerScreen = ({ onTransfer }) => {
  const movements = [
    { t: "14:38", type: "Cash sale",      ord: "ORD-...00482", amt: 4280,  by: "Adeel" },
    { t: "14:21", type: "Cash sale",      ord: "ORD-...00479", amt: 1240,  by: "Adeel" },
    { t: "14:02", type: "Refund",         ord: "ORD-...00472", amt: -680,  by: "Adeel" },
    { t: "13:45", type: "Cash sale",      ord: "ORD-...00471", amt: 920,   by: "Adeel" },
    { t: "13:18", type: "Drawer→Vault",   ord: "MOV-3318",     amt: -10000, by: "Fahad" },
    { t: "12:50", type: "Payout",         ord: "PYT-118",      amt: -800,  by: "Adeel" },
    { t: "12:24", type: "Cash sale",      ord: "ORD-...00465", amt: 2150,  by: "Adeel" },
    { t: "11:40", type: "Manual correction", ord: "ADJ-44",   amt: -50,   by: "Fahad" },
    { t: "09:00", type: "Opening float",  ord: "—",            amt: 15000, by: "Adeel" },
  ];

  const opening = 15000;
  const cashIn = 38200, refunds = -1200, payouts = -800, drawerToVault = -10000;
  const expected = opening + cashIn + refunds + payouts + drawerToVault;

  return (
    <div style={{ padding: 28, height: '100%', overflow: 'auto' }}>
      <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: 20 }}>
        <div>
          <div style={{ fontSize: 11.5, color: 'var(--ink-400)', letterSpacing: '.08em', textTransform: 'uppercase', fontWeight: 500 }}>Cash drawer · POS-01 · Shift S-4218</div>
          <h1 style={{ margin: '4px 0 0', fontSize: 26, fontWeight: 600, letterSpacing: '-.02em' }}>Drawer activity</h1>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          <Btn variant="default" icon="print">Print snapshot</Btn>
          <Btn variant="default" icon="plus">Add payout</Btn>
          <Btn variant="primary" icon="vault" onClick={onTransfer}>Move to Vault</Btn>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: 12, marginBottom: 20 }}>
        <Stat label="Opening" value={fmtT(opening)} accent="var(--ink-300)"/>
        <Stat label="Cash sales" value={fmtT(cashIn)} accent="var(--green-600)" delta="+24 orders" deltaTone="success"/>
        <Stat label="Refunds" value={fmtT(refunds)} accent="var(--red-600)" sub="2 lines"/>
        <Stat label="Payouts + transfers" value={fmtT(payouts + drawerToVault)} accent="var(--amber-600)" sub="3 events"/>
        <Stat label="Expected drawer" value={fmtT(expected)} accent="var(--navy-800)"/>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 360px', gap: 20 }}>
        <Card padding={0}>
          <div style={{ padding: '14px 18px', borderBottom: '1px solid var(--line)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div style={{ fontSize: 14, fontWeight: 600 }}>Movement ledger</div>
            <div style={{ display: 'flex', gap: 8 }}>
              <Select options={["All movements","Cash sales only","Transfers only","Adjustments"]} value="All movements" onChange={()=>{}}/>
              <Btn variant="default" size="sm" icon="filter">Filter</Btn>
            </div>
          </div>
          <Table
            dense
            columns={[
              { key: "t", label: "Time", mono: true, width: 80 },
              { key: "type", label: "Type" },
              { key: "ord", label: "Reference", mono: true, muted: true },
              { key: "by", label: "By" },
              { key: "amt", label: "Amount", align: 'right', mono: true, render: r => (
                <span style={{ color: r.amt < 0 ? 'var(--red-700)' : 'var(--green-700)', fontWeight: 500 }}>
                  {r.amt < 0 ? "−" : "+"}PKR {Math.abs(r.amt).toLocaleString()}
                </span>
              ) },
            ]}
            rows={movements}
          />
        </Card>

        <Card padding={20}>
          <SectionTitle>Drawer health</SectionTitle>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, padding: '12px 14px', background: 'var(--green-50)', borderRadius: 10, marginBottom: 14 }}>
            <Icon name="check" size={20} color="var(--green-700)"/>
            <div style={{ fontSize: 13, color: 'var(--green-700)' }}>
              <div style={{ fontWeight: 500 }}>Drawer ready</div>
              <div style={{ fontSize: 11.5 }}>Connected · Last kick 14:38</div>
            </div>
          </div>

          <SectionTitle>Skim recommendations</SectionTitle>
          <div style={{ padding: 14, background: 'var(--amber-50)', borderRadius: 10, fontSize: 12.5, color: 'var(--amber-700)', display: 'flex', gap: 10 }}>
            <Icon name="warning" size={16} color="var(--amber-700)"/>
            <div>Drawer holds <span className="num" style={{ fontWeight: 600 }}>PKR 41,200</span> — above safe limit of <span className="num">PKR 30,000</span>. Move to Vault recommended.</div>
          </div>

          <div style={{ marginTop: 18 }}>
            <SectionTitle>Recent corrections</SectionTitle>
            <div style={{ fontSize: 12.5 }}>
              <div style={{ padding: '8px 0', borderBottom: '1px solid var(--line-soft)', display: 'flex', justifyContent: 'space-between' }}>
                <span>11:40 · Float correction</span>
                <span style={{ color: 'var(--red-700)' }}>−PKR 50</span>
              </div>
              <div style={{ padding: '8px 0', color: 'var(--ink-500)' }}>
                Approved by Fahad · "Counted short during mid-shift skim"
              </div>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
};

const DrawerToVaultScreen = ({ onBack, onConfirm }) => {
  const [amount, setAmount] = React.useState("25000");
  const [vault, setVault] = React.useState("Main Branch Vault");
  const [reason, setReason] = React.useState("Mid-shift skim");
  const [escort, setEscort] = React.useState("E1043 — Fahad");

  return (
    <div style={{ padding: 32, height: '100%', overflow: 'auto' }}>
      <button onClick={onBack} style={{ background: 'transparent', border: 'none', cursor: 'pointer', color: 'var(--ink-500)', fontSize: 13, display: 'inline-flex', alignItems: 'center', gap: 6, marginBottom: 12 }}>
        <Icon name="chevronL" size={16}/> Back to drawer
      </button>
      <div style={{ marginBottom: 20 }}>
        <div style={{ fontSize: 11.5, color: 'var(--ink-400)', letterSpacing: '.08em', textTransform: 'uppercase', fontWeight: 500 }}>Cash movement</div>
        <h1 style={{ margin: '4px 0 0', fontSize: 26, fontWeight: 600, letterSpacing: '-.02em' }}>Drawer → Vault</h1>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 420px', gap: 24 }}>
        <Card padding={24}>
          {/* Flow header */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 40px 1fr', gap: 16, alignItems: 'center', marginBottom: 24, padding: 18, background: 'var(--surface-2)', borderRadius: 12 }}>
            <div>
              <div style={{ fontSize: 11.5, color: 'var(--ink-500)', textTransform: 'uppercase', letterSpacing: '.05em', marginBottom: 4 }}>Source</div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <div style={{ width: 36, height: 36, borderRadius: 8, background: '#fff', border: '1px solid var(--line)', display: 'grid', placeItems: 'center' }}>
                  <Icon name="drawer" size={18} color="var(--navy-800)"/>
                </div>
                <div>
                  <div style={{ fontWeight: 500 }}>POS-01 Drawer</div>
                  <div className="num" style={{ fontSize: 11.5, color: 'var(--ink-500)' }}>Shift S-4218 · Adeel</div>
                </div>
              </div>
            </div>
            <div style={{ display: 'grid', placeItems: 'center' }}>
              <div style={{ width: 32, height: 32, borderRadius: 99, background: 'var(--navy-800)', display: 'grid', placeItems: 'center' }}>
                <Icon name="arrowR" size={16} color="#fff"/>
              </div>
            </div>
            <div>
              <div style={{ fontSize: 11.5, color: 'var(--ink-500)', textTransform: 'uppercase', letterSpacing: '.05em', marginBottom: 4 }}>Destination</div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <div style={{ width: 36, height: 36, borderRadius: 8, background: '#fff', border: '1px solid var(--line)', display: 'grid', placeItems: 'center' }}>
                  <Icon name="vault" size={18} color="var(--navy-800)"/>
                </div>
                <div>
                  <Select value={vault} onChange={setVault} options={["Main Branch Vault","City Branch Vault","Airport Branch Vault"]} style={{ height: 32, padding: '0 8px', fontSize: 13 }}/>
                  <div className="num" style={{ fontSize: 11.5, color: 'var(--ink-500)', marginTop: 4 }}>Balance · PKR 482,500</div>
                </div>
              </div>
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 16 }}>
            <Field label="Amount (PKR)">
              <Input full style={{ height: 56, fontSize: 22 }} value={amount} onChange={(e) => setAmount(e.target.value.replace(/\D/g,''))}/>
            </Field>
            <Field label="Reason">
              <Select full value={reason} onChange={setReason} options={["Mid-shift skim","End-of-shift drop","Overage correction","Manager request"]}/>
            </Field>
          </div>

          <Field label="Escort / second party (recommended)" style={{ marginBottom: 16 }}>
            <Select full value={escort} onChange={setEscort} options={["E1043 — Fahad","E1046 — Hira","None (single-party)"]}/>
          </Field>

          <Field label="Reference number">
            <Input full placeholder="Auto: MOV-Drawer-MB-260519-0014" disabled value="MOV-Drawer-MB-260519-0014"/>
          </Field>

          <div style={{ marginTop: 18, padding: 14, background: 'var(--amber-50)', borderRadius: 10, fontSize: 12.5, color: 'var(--amber-700)', display: 'flex', gap: 10 }}>
            <Icon name="warning" size={16} color="var(--amber-700)"/>
            <div>Recorded as an append-only <span style={{ fontFamily: 'var(--mono)' }}>CashAccountMovement</span> · type <span style={{ fontFamily: 'var(--mono)' }}>DrawerToVault</span>. Will require manager approval before settlement.</div>
          </div>
        </Card>

        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <Card padding={20}>
            <div style={{ fontSize: 11.5, color: 'var(--ink-400)', textTransform: 'uppercase', letterSpacing: '.05em', fontWeight: 500 }}>Movement summary</div>
            <div className="num" style={{ fontSize: 42, fontWeight: 600, letterSpacing: '-.02em', marginTop: 6 }}>{fmtT(amount || 0)}</div>
            <div style={{ marginTop: 16, fontSize: 13 }}>
              <KV k="Drawer balance now" v="PKR 41,200" mono/>
              <KV k="After transfer" v={fmtT(41200 - Number(amount || 0))} mono color="var(--ink-400)"/>
              <KV k="Vault balance after" v={fmtT(482500 + Number(amount || 0))} mono divider/>
            </div>
          </Card>

          <Card padding={18} style={{ background: 'var(--surface-2)', borderColor: 'transparent' }}>
            <div style={{ fontSize: 12, color: 'var(--ink-500)', textTransform: 'uppercase', letterSpacing: '.05em', marginBottom: 8 }}>Authorization chain</div>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, fontSize: 13 }}>
                <Dot tone="success"/> Recorded by <span style={{ fontWeight: 500 }}>Adeel</span> · 14:42
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, fontSize: 13 }}>
                <Dot tone="amber"/> Awaiting manager approval
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, fontSize: 13, color: 'var(--ink-400)' }}>
                <Dot tone="neutral"/> Vault verification
              </div>
            </div>
          </Card>

          <Btn variant="primary" size="xl" full onClick={onConfirm}>
            <Icon name="check" size={20}/> Create transfer
          </Btn>
          <Btn variant="default" full icon="print">Print transfer slip</Btn>
        </div>
      </div>
    </div>
  );
};

const VaultToBankScreen = ({ onBack, onConfirm }) => {
  const [amount, setAmount] = React.useState("250000");
  const [bank, setBank] = React.useState("HBL Collection Account");
  const [ref, setRef] = React.useState("DEP-HBL-260519-0006");

  return (
    <div style={{ padding: 32, height: '100%', overflow: 'auto' }}>
      <button onClick={onBack} style={{ background: 'transparent', border: 'none', cursor: 'pointer', color: 'var(--ink-500)', fontSize: 13, display: 'inline-flex', alignItems: 'center', gap: 6, marginBottom: 12 }}>
        <Icon name="chevronL" size={16}/> Back
      </button>
      <div style={{ marginBottom: 20 }}>
        <div style={{ fontSize: 11.5, color: 'var(--ink-400)', letterSpacing: '.08em', textTransform: 'uppercase', fontWeight: 500 }}>Cash deposit</div>
        <h1 style={{ margin: '4px 0 0', fontSize: 26, fontWeight: 600, letterSpacing: '-.02em' }}>Vault → Bank</h1>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24, marginBottom: 24 }}>
        <Card padding={24}>
          <SectionTitle>Source vault</SectionTitle>
          <Select full value="Main Branch Vault" options={["Main Branch Vault","City Branch Vault","Airport Branch Vault"]} onChange={()=>{}}/>
          <div style={{ marginTop: 16, padding: 16, background: 'var(--surface-2)', borderRadius: 10 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12.5, color: 'var(--ink-500)', marginBottom: 6 }}>
              <span>Vault balance</span><span className="num" style={{ color: 'var(--ink-900)', fontWeight: 500 }}>PKR 482,500</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12.5, color: 'var(--ink-500)' }}>
              <span>Pending in transit</span><span className="num">PKR 18,200</span>
            </div>
            <div style={{ height: 6, marginTop: 12, background: '#fff', borderRadius: 99, overflow: 'hidden' }}>
              <div style={{ width: '62%', height: '100%', background: 'var(--navy-800)' }}/>
            </div>
            <div style={{ marginTop: 8, fontSize: 11.5, color: 'var(--ink-500)' }}>62% of vault holding limit (PKR 800,000)</div>
          </div>
        </Card>

        <Card padding={24}>
          <SectionTitle>Destination bank</SectionTitle>
          <Select full value={bank} onChange={setBank} options={["HBL Collection Account","Meezan Deposit Account"]}/>
          <div style={{ marginTop: 16, padding: 16, background: 'var(--surface-2)', borderRadius: 10, fontSize: 12.5 }}>
            <KV k="Account holder" v="Enterprise Retail Co."/>
            <KV k="Account #" v="•••• 4892" mono/>
            <KV k="IBAN" v="PK•• HABB •••• 4892" mono/>
            <KV k="Currency" v="PKR"/>
          </div>
        </Card>
      </div>

      <Card padding={24}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 18 }}>
          <Field label="Deposit amount (PKR)">
            <Input full style={{ height: 56, fontSize: 22 }} value={amount} onChange={(e) => setAmount(e.target.value.replace(/\D/g,''))}/>
          </Field>
          <Field label="Bank reference #">
            <Input full value={ref} onChange={(e) => setRef(e.target.value)}/>
          </Field>
          <Field label="Deposit slip">
            <button style={{
              height: 44, border: '1px dashed var(--line-hard)', borderRadius: 8, background: '#fff',
              display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 8, cursor: 'pointer', color: 'var(--ink-600)', fontSize: 13,
            }}><Icon name="receipt" size={16}/> Attach photo / PDF</button>
          </Field>
        </div>

        <div style={{ marginTop: 22 }}>
          <SectionTitle>Verification & status</SectionTitle>
          <div style={{ display: 'flex', gap: 12 }}>
            {[
              { k: "Recorded", who: "Fahad · Manager", time: "14:42", state: "done" },
              { k: "Cash counted", who: "Hira · Manager", time: "14:55", state: "active" },
              { k: "Slip uploaded", who: "—", time: "—", state: "pending" },
              { k: "Verified by Finance", who: "—", time: "—", state: "pending" },
            ].map((s, i) => (
              <div key={i} style={{
                flex: 1, padding: 14, borderRadius: 10,
                background: s.state === 'done' ? 'var(--green-50)' : s.state === 'active' ? 'var(--blue-50)' : 'var(--surface-2)',
                border: '1px solid ' + (s.state === 'done' ? 'var(--green-100)' : s.state === 'active' ? 'var(--blue-100)' : 'var(--line)'),
              }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
                  <Dot tone={s.state === 'done' ? 'success' : s.state === 'active' ? 'blue' : 'neutral'}/>
                  <div style={{ fontSize: 12.5, fontWeight: 500 }}>{s.k}</div>
                </div>
                <div style={{ fontSize: 12, color: 'var(--ink-500)' }}>{s.who}</div>
                <div className="num" style={{ fontSize: 11.5, color: 'var(--ink-400)', marginTop: 2 }}>{s.time}</div>
              </div>
            ))}
          </div>
        </div>
      </Card>

      <div style={{ marginTop: 20, display: 'flex', gap: 12, justifyContent: 'flex-end' }}>
        <Btn variant="default" icon="print">Print deposit slip</Btn>
        <Btn variant="primary" icon="check" size="lg" onClick={onConfirm}>Record deposit</Btn>
      </div>
    </div>
  );
};

Object.assign(window, { CashDrawerScreen, DrawerToVaultScreen, VaultToBankScreen });
