// Admin: Item management, Employee/Terminal management, Admin Dashboard

const ItemManagementScreen = () => {
  const [selectedItem, setSelectedItem] = React.useState(ITEMS[0]);
  const [query, setQuery] = React.useState("");
  const filtered = ITEMS.filter(i =>
    query === "" || i.name.toLowerCase().includes(query.toLowerCase()) || i.sku.toLowerCase().includes(query.toLowerCase())
  );

  return (
    <div style={{ display: 'grid', gridTemplateColumns: '320px 1fr 380px', height: '100%', overflow: 'hidden' }}>
      {/* Left: item list */}
      <div style={{ borderRight: '1px solid var(--line)', display: 'flex', flexDirection: 'column', background: '#fff' }}>
        <div style={{ padding: '16px 18px 12px' }}>
          <div style={{ fontSize: 11.5, color: 'var(--ink-400)', letterSpacing: '.08em', textTransform: 'uppercase', marginBottom: 6 }}>Catalog</div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 14 }}>
            <h2 style={{ margin: 0, fontSize: 18, fontWeight: 600 }}>Items</h2>
            <span className="num" style={{ fontSize: 12.5, color: 'var(--ink-500)' }}>{ITEMS.length} of 412</span>
          </div>
          <Input icon="search" full placeholder="Search SKU, name, identifier..." value={query} onChange={(e) => setQuery(e.target.value)}/>
        </div>
        <div style={{ flex: 1, overflow: 'auto', padding: '0 8px' }}>
          {filtered.map(i => (
            <button key={i.id} onClick={() => setSelectedItem(i)} style={{
              display: 'grid', gridTemplateColumns: '36px 1fr auto', gap: 10, alignItems: 'center',
              width: '100%', padding: '10px 10px', borderRadius: 8, marginBottom: 2,
              border: 'none', cursor: 'pointer', textAlign: 'left',
              background: selectedItem?.id === i.id ? 'var(--blue-50)' : 'transparent',
            }}>
              <Placeholder label="" ratio="1/1" style={{ height: 36, width: 36 }}/>
              <div style={{ minWidth: 0 }}>
                <div style={{ fontSize: 13, fontWeight: 500, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{i.name}</div>
                <div className="num" style={{ fontSize: 11, color: 'var(--ink-500)' }}>{i.sku}</div>
              </div>
              <div style={{ textAlign: 'right' }}>
                <div className="num" style={{ fontSize: 12.5, fontWeight: 500 }}>{fmtT(i.price)}</div>
                <div className="num" style={{ fontSize: 10.5, color: i.stock < 20 ? 'var(--amber-700)' : 'var(--ink-400)' }}>{i.stock} {i.unit}</div>
              </div>
            </button>
          ))}
        </div>
        <div style={{ padding: 12, borderTop: '1px solid var(--line)' }}>
          <Btn variant="primary" full icon="plus">Add new item</Btn>
        </div>
      </div>

      {/* Middle: item editor */}
      <div style={{ overflow: 'auto', background: 'var(--surface)' }}>
        <div style={{ padding: 28 }}>
          <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: 22 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
              <Placeholder label={selectedItem.sku} ratio="1/1" style={{ height: 72, width: 72 }}/>
              <div>
                <div style={{ fontSize: 11.5, color: 'var(--ink-400)', letterSpacing: '.08em', textTransform: 'uppercase' }}>Item</div>
                <h1 style={{ margin: '4px 0 4px', fontSize: 24, fontWeight: 600, letterSpacing: '-.01em' }}>{selectedItem.name}</h1>
                <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                  <Badge tone="success" dot>Active</Badge>
                  <Badge tone="outline">{CATEGORIES.find(c => c.id === selectedItem.cat)?.name || selectedItem.cat}</Badge>
                  <span className="num" style={{ fontSize: 12, color: 'var(--ink-500)' }}>{selectedItem.id}</span>
                </div>
              </div>
            </div>
            <div style={{ display: 'flex', gap: 8 }}>
              <Btn variant="default" icon="edit">Edit</Btn>
              <Btn variant="dangerOutline" icon="trash">Discontinue</Btn>
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 18 }}>
            <Card padding={20}>
              <SectionTitle>Identity</SectionTitle>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
                <Field label="SKU"><Input full value={selectedItem.sku} disabled/></Field>
                <Field label="Item type"><Select full value="Stocked good" options={["Stocked good","Service","Voucher","Variable weight"]} onChange={()=>{}}/></Field>
                <Field label="Name" style={{ gridColumn: 'span 2' }}><Input full value={selectedItem.name} disabled/></Field>
                <Field label="Category"><Select full value={CATEGORIES.find(c => c.id === selectedItem.cat)?.name || ""} options={CATEGORIES.map(c => c.name)} onChange={()=>{}}/></Field>
                <Field label="Unit of measure"><Select full value={selectedItem.unit} options={["ea","kg","g","L","ml","pk"]} onChange={()=>{}}/></Field>
              </div>
            </Card>

            <Card padding={20}>
              <SectionTitle>Identifiers</SectionTitle>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                {[
                  { kind: "EAN-13", code: "8964000123478", primary: true },
                  { kind: "EAN-13", code: "8964000123485", primary: false },
                  { kind: "QR", code: "PSKU:" + selectedItem.sku, primary: false },
                ].map((c, i) => (
                  <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '8px 12px', background: 'var(--surface-2)', borderRadius: 8 }}>
                    <Badge tone={c.primary ? "navy" : "outline"}>{c.kind}</Badge>
                    <span className="num" style={{ flex: 1, fontSize: 12.5 }}>{c.code}</span>
                    {c.primary && <Badge tone="success" dot>Primary</Badge>}
                  </div>
                ))}
                <Btn variant="default" size="sm" icon="plus" style={{ marginTop: 4 }}>Add identifier</Btn>
              </div>
            </Card>

            <Card padding={20}>
              <SectionTitle action={<Badge tone="blue">v14 · Active</Badge>}>Price & tax</SectionTitle>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
                <Field label="Standard price (PKR)"><Input full value={selectedItem.price.toLocaleString()}/></Field>
                <Field label="Cost (PKR)"><Input full value={Math.round(selectedItem.price * 0.7).toLocaleString()}/></Field>
                <Field label="Tax rule"><Select full value={selectedItem.tax === 0 ? "Zero-rated" : `${selectedItem.tax}% Sales Tax`} options={["Zero-rated","5% Sales Tax","12% Sales Tax","18% Sales Tax"]} onChange={()=>{}}/></Field>
                <Field label="Pricing mode"><Select full value="Tax-exclusive" options={["Tax-exclusive","Tax-inclusive"]} onChange={()=>{}}/></Field>
              </div>
              <div style={{ marginTop: 14, padding: 10, background: 'var(--surface-2)', borderRadius: 8, fontSize: 12.5, color: 'var(--ink-500)' }}>
                Effective from <span className="num">12 May 2026</span> · Previous price <span className="num" style={{ color: 'var(--ink-700)' }}>PKR {Math.round(selectedItem.price * 1.05).toLocaleString()}</span>
              </div>
            </Card>

            <Card padding={20}>
              <SectionTitle action={<Btn variant="ghost" size="sm">View ledger</Btn>}>Stock by location</SectionTitle>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                {[
                  { loc: "Main Branch", qty: selectedItem.stock, ok: true },
                  { loc: "City Branch", qty: Math.round(selectedItem.stock * 0.6), ok: true },
                  { loc: "Airport Branch", qty: Math.round(selectedItem.stock * 0.2), ok: false },
                ].map((s, i) => (
                  <div key={i} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '10px 12px', background: 'var(--surface-2)', borderRadius: 8 }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                      <Icon name="location" size={14} color="var(--ink-500)"/>
                      <span style={{ fontSize: 13 }}>{s.loc}</span>
                    </div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                      <span className="num" style={{ fontSize: 13, fontWeight: 500 }}>{s.qty} {selectedItem.unit}</span>
                      <Badge tone={s.ok ? "success" : "amber"} dot>{s.ok ? "OK" : "Low"}</Badge>
                    </div>
                  </div>
                ))}
              </div>
            </Card>
          </div>
        </div>
      </div>

      {/* Right: activity */}
      <div style={{ borderLeft: '1px solid var(--line)', background: '#fff', overflow: 'auto' }}>
        <div style={{ padding: '18px 20px', borderBottom: '1px solid var(--line)' }}>
          <div style={{ fontSize: 11.5, color: 'var(--ink-400)', letterSpacing: '.08em', textTransform: 'uppercase' }}>Activity</div>
          <h3 style={{ margin: '4px 0 0', fontSize: 16, fontWeight: 600 }}>Recent changes</h3>
        </div>
        <div style={{ padding: '14px 20px' }}>
          {[
            { t: "2h ago", who: "Store Admin", what: "Updated price PKR 1,950 → PKR 1,850", tone: "blue" },
            { t: "1d ago", who: "Store Admin", what: "Added barcode 8964000123485", tone: "success" },
            { t: "3d ago", who: "Sync · Central", what: "Promoted price list v14 to Active", tone: "neutral" },
            { t: "5d ago", who: "Fahad", what: "Stock adjusted +24 at Main Branch", tone: "success" },
            { t: "8d ago", who: "Store Admin", what: "Item created", tone: "neutral" },
          ].map((a, i) => (
            <div key={i} style={{ display: 'grid', gridTemplateColumns: '14px 1fr', gap: 10, padding: '10px 0', borderBottom: '1px solid var(--line-soft)' }}>
              <div style={{ paddingTop: 6 }}><Dot tone={a.tone}/></div>
              <div>
                <div style={{ fontSize: 13 }}>{a.what}</div>
                <div style={{ fontSize: 11.5, color: 'var(--ink-500)', marginTop: 2 }}>{a.who} · {a.t}</div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

const EmployeeTerminalScreen = () => {
  const [tab, setTab] = React.useState("terminals");
  return (
    <div style={{ padding: 28, height: '100%', overflow: 'auto', background: 'var(--surface)' }}>
      <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: 18 }}>
        <div>
          <div style={{ fontSize: 11.5, color: 'var(--ink-400)', letterSpacing: '.08em', textTransform: 'uppercase' }}>Operations</div>
          <h1 style={{ margin: '4px 0 0', fontSize: 26, fontWeight: 600, letterSpacing: '-.02em' }}>People & devices</h1>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          <Btn variant="default" icon="download">Export</Btn>
          {tab === "terminals" ? <Btn variant="primary" icon="plus">Provision terminal</Btn> : <Btn variant="primary" icon="plus">Add employee</Btn>}
        </div>
      </div>

      <div style={{ display: 'flex', gap: 4, marginBottom: 18, borderBottom: '1px solid var(--line)' }}>
        {[
          { id: "terminals", label: "Terminals", count: TERMINALS.length },
          { id: "employees", label: "Employees", count: EMPLOYEES.length },
          { id: "roles",     label: "Roles & permissions", count: 5 },
        ].map(t => (
          <button key={t.id} onClick={() => setTab(t.id)} style={{
            padding: '10px 16px', border: 'none', background: 'transparent', cursor: 'pointer',
            fontSize: 13.5, fontWeight: 500,
            color: tab === t.id ? 'var(--navy-800)' : 'var(--ink-500)',
            borderBottom: '2px solid ' + (tab === t.id ? 'var(--navy-800)' : 'transparent'),
            marginBottom: -1,
          }}>{t.label} <span style={{ color: 'var(--ink-400)', fontFamily: 'var(--mono)', marginLeft: 4 }}>{t.count}</span></button>
        ))}
      </div>

      {tab === "terminals" && (
        <>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12, marginBottom: 18 }}>
            <Stat label="Total terminals" value={TERMINALS.length}/>
            <Stat label="Online" value="3" accent="var(--green-600)"/>
            <Stat label="Offline" value="1" accent="var(--amber-600)" sub="POS-04 · 14m"/>
            <Stat label="Deprovisioning" value="1" accent="var(--red-600)"/>
          </div>
          <Card padding={0}>
            <Table
              columns={[
                { key: "id", label: "Terminal", render: r => (
                  <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                    <Icon name="terminal" size={18} color="var(--navy-800)"/>
                    <div>
                      <div className="num" style={{ fontWeight: 500 }}>{r.id}</div>
                      <div style={{ fontSize: 11.5, color: 'var(--ink-500)' }}>{r.location}</div>
                    </div>
                  </div>
                )},
                { key: "status", label: "Status", render: r => (
                  <Badge tone={r.status === 'online' ? 'success' : r.status === 'offline' ? 'amber' : 'danger'} dot>
                    {r.status}
                  </Badge>
                )},
                { key: "operator", label: "Operator" },
                { key: "shift", label: "Shift", mono: true },
                { key: "outbox", label: "Outbox", align: 'right', mono: true, render: r => (
                  <span style={{ color: r.outbox === 0 ? 'var(--green-700)' : r.outbox > 5 ? 'var(--red-700)' : 'var(--amber-700)', fontWeight: 500 }}>{r.outbox}</span>
                )},
                { key: "lastSync", label: "Last sync", muted: true },
                { key: "actions", label: "", align: 'right', render: () => (
                  <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 6 }}>
                    <Btn variant="ghost" size="sm" icon="sync">Force sync</Btn>
                    <Btn variant="ghost" size="sm">Manage</Btn>
                  </div>
                )},
              ]}
              rows={TERMINALS}
            />
          </Card>
        </>
      )}

      {tab === "employees" && (
        <>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12, marginBottom: 18 }}>
            <Stat label="Employees" value={EMPLOYEES.length}/>
            <Stat label="Active" value="6" accent="var(--green-600)"/>
            <Stat label="Suspended" value="1" accent="var(--amber-600)"/>
            <Stat label="Managers" value="2"/>
          </div>
          <Card padding={0}>
            <Table
              columns={[
                { key: "name", label: "Employee", render: r => (
                  <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                    <div style={{ width: 32, height: 32, borderRadius: 99, background: 'var(--navy-800)', color: '#fff', display: 'grid', placeItems: 'center', fontSize: 12, fontWeight: 600 }}>
                      {r.name.slice(0, 2).toUpperCase()}
                    </div>
                    <div>
                      <div style={{ fontWeight: 500 }}>{r.name}</div>
                      <div className="num" style={{ fontSize: 11.5, color: 'var(--ink-500)' }}>{r.id}</div>
                    </div>
                  </div>
                )},
                { key: "role", label: "Role", render: r => <Badge tone={r.role === 'Admin' ? 'navy' : r.role === 'Manager' ? 'blue' : 'outline'}>{r.role}</Badge> },
                { key: "location", label: "Locations" },
                { key: "shifts", label: "Shifts (90d)", align: 'right', mono: true },
                { key: "lastActive", label: "Last active", muted: true },
                { key: "status", label: "Status", render: r => <Badge tone={r.status === 'active' ? 'success' : 'amber'} dot>{r.status}</Badge> },
                { key: "actions", label: "", align: 'right', render: () => <Btn variant="ghost" size="sm" icon="edit">Edit</Btn> },
              ]}
              rows={EMPLOYEES}
            />
          </Card>
        </>
      )}

      {tab === "roles" && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 16 }}>
          {[
            { name: "Cashier", desc: "Takes orders, runs checkout, requests manager approvals.", count: 4, perms: ["Open shift","Run checkout","Refund (limited)","Void item (limited)"] },
            { name: "Manager", desc: "Approves protected actions, reconciles cash, runs reports.", count: 2, perms: ["All Cashier","Approve overrides","Drawer→Vault","Run Z-Report","Edit prices (limited)"] },
            { name: "Location Admin", desc: "Manages catalog and employees within one branch.", count: 1, perms: ["All Manager","Add employees","Manage catalog","Manage devices"] },
            { name: "Finance", desc: "Verifies bank deposits and reconciliations.", count: 1, perms: ["Vault→Bank verify","Read all reports","Export ledgers"] },
            { name: "Tenant Admin", desc: "Full control over the tenant and all locations.", count: 1, perms: ["All permissions","Tenant settings","Provisioning"] },
          ].map(r => (
            <Card key={r.name} padding={20}>
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 10 }}>
                <div style={{ fontSize: 15, fontWeight: 600 }}>{r.name}</div>
                <Badge tone="outline">{r.count} assigned</Badge>
              </div>
              <p style={{ margin: 0, color: 'var(--ink-500)', fontSize: 12.5, lineHeight: 1.5 }}>{r.desc}</p>
              <div style={{ marginTop: 14, display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                {r.perms.map(p => <Badge key={p} tone="outline" style={{ fontSize: 11 }}>{p}</Badge>)}
              </div>
              <div style={{ marginTop: 14, display: 'flex', gap: 8 }}>
                <Btn variant="ghost" size="sm" icon="edit">Edit</Btn>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
};

const AdminDashboard = ({ go }) => (
  <div style={{ padding: 28, height: '100%', overflow: 'auto', background: 'var(--surface)' }}>
    <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: 18 }}>
      <div>
        <div style={{ fontSize: 11.5, color: 'var(--ink-400)', letterSpacing: '.08em', textTransform: 'uppercase' }}>Tenant overview · Enterprise Retail Co.</div>
        <h1 style={{ margin: '4px 0 0', fontSize: 28, fontWeight: 600, letterSpacing: '-.02em' }}>Good afternoon, Store Admin</h1>
        <div style={{ fontSize: 13, color: 'var(--ink-500)', marginTop: 4 }}>3 locations · 9 terminals · business date <span className="num">19 May 2026</span></div>
      </div>
      <div style={{ display: 'flex', gap: 10 }}>
        <Select options={["All locations","Main Branch","City Branch","Airport Branch"]} value="All locations" onChange={()=>{}}/>
        <Btn variant="default" icon="settings">Tenant settings</Btn>
      </div>
    </div>

    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12, marginBottom: 20 }}>
      <Stat label="Today's gross" value="PKR 1,284,610" delta="+6.4%" deltaTone="success" sub="vs yesterday"/>
      <Stat label="Orders today" value="842" sub="avg basket PKR 1,525"/>
      <Stat label="Open shifts" value="5" accent="var(--green-600)" sub="across 3 locations"/>
      <Stat label="Pending approvals" value="3" accent="var(--amber-600)" sub="2 voids · 1 transfer"/>
    </div>

    <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: 20, marginBottom: 20 }}>
      <Card padding={22}>
        <SectionTitle action={
          <div style={{ display: 'flex', gap: 6 }}>
            <Btn variant="ghost" size="sm">Today</Btn>
            <Btn variant="default" size="sm">7 days</Btn>
            <Btn variant="ghost" size="sm">30 days</Btn>
          </div>
        }>Sales by location</SectionTitle>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 14, marginTop: 6 }}>
          {[
            { name: "Main Branch", v: 632400, c: "var(--blue-600)", pct: 100 },
            { name: "City Branch", v: 458200, c: "var(--green-600)", pct: 72 },
            { name: "Airport Branch", v: 194010, c: "var(--amber-600)", pct: 31 },
          ].map(l => (
            <div key={l.name}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6, fontSize: 13 }}>
                <span style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
                  <Icon name="location" size={14} color={l.c}/> {l.name}
                </span>
                <span className="num" style={{ fontWeight: 500 }}>{fmtT(l.v)}</span>
              </div>
              <div style={{ height: 10, background: 'var(--surface-2)', borderRadius: 99 }}>
                <div style={{ width: `${l.pct}%`, height: '100%', background: l.c, borderRadius: 99 }}/>
              </div>
            </div>
          ))}
        </div>
      </Card>

      <Card padding={22}>
        <SectionTitle>Cash position</SectionTitle>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          {CASH_ACCOUNTS.map(a => (
            <div key={a.id} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid var(--line-soft)' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <Icon name={a.type === 'Vault' ? 'vault' : 'bank'} size={16} color="var(--ink-500)"/>
                <div>
                  <div style={{ fontSize: 13, fontWeight: 500 }}>{a.name}</div>
                  <div style={{ fontSize: 11.5, color: 'var(--ink-500)' }}>{a.type} · {a.location}</div>
                </div>
              </div>
              <div className="num" style={{ fontSize: 14, fontWeight: 600 }}>{fmtT(a.balance)}</div>
            </div>
          ))}
        </div>
      </Card>
    </div>

    <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: 20 }}>
      <Card padding={0}>
        <div style={{ padding: '14px 18px', borderBottom: '1px solid var(--line)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div style={{ fontSize: 14, fontWeight: 600 }}>Pending approvals</div>
          <Btn variant="ghost" size="sm">View all <Icon name="chevronR" size={12}/></Btn>
        </div>
        <Table
          dense
          columns={[
            { key: "kind", label: "Action", render: r => <Badge tone={r.tone}>{r.kind}</Badge> },
            { key: "ref", label: "Reference", mono: true },
            { key: "by", label: "Requested by" },
            { key: "loc", label: "Location", muted: true },
            { key: "amt", label: "Amount", align: 'right', mono: true },
            { key: "actions", label: "", align: 'right', render: () => (
              <div style={{ display: 'flex', gap: 6, justifyContent: 'flex-end' }}>
                <Btn variant="ghost" size="sm">Review</Btn>
                <Btn variant="success" size="sm" icon="check">Approve</Btn>
              </div>
            )},
          ]}
          rows={[
            { kind: "Void", tone: "danger",  ref: "ORD-MB-...00479", by: "Adeel · POS-01", loc: "Main Branch", amt: "PKR 530" },
            { kind: "Refund", tone: "amber", ref: "ORD-CB-...00318", by: "Sana · POS-02",  loc: "City Branch",  amt: "PKR 1,240" },
            { kind: "Drawer→Vault", tone: "blue", ref: "MOV-3318",   by: "Adeel · POS-01", loc: "Main Branch", amt: "PKR 25,000" },
          ]}
        />
      </Card>

      <Card padding={22}>
        <SectionTitle>Sync & device health</SectionTitle>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          <div style={{ padding: 14, background: 'var(--green-50)', borderRadius: 10 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
              <span style={{ fontSize: 12.5, color: 'var(--green-700)', fontWeight: 500 }}>Sync healthy</span>
              <span className="num" style={{ fontSize: 12, color: 'var(--green-700)' }}>98.4% in &lt; 2s</span>
            </div>
            <div style={{ fontSize: 11.5, color: 'var(--green-700)' }}>8 of 9 terminals streaming · 1 catching up</div>
          </div>
          {[
            { t: "POS-04 · City Branch", st: "Offline 14m · 9 in outbox", tone: "amber" },
            { t: "POS-05 · Airport",     st: "Deprovisioning in progress", tone: "danger" },
            { t: "Printer · POS-01",     st: "Low paper", tone: "amber" },
          ].map((d, i) => (
            <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '8px 0', borderTop: '1px solid var(--line-soft)' }}>
              <Dot tone={d.tone}/>
              <div style={{ flex: 1 }}>
                <div style={{ fontSize: 12.5, fontWeight: 500 }}>{d.t}</div>
                <div style={{ fontSize: 11.5, color: 'var(--ink-500)' }}>{d.st}</div>
              </div>
            </div>
          ))}
        </div>
      </Card>
    </div>
  </div>
);

Object.assign(window, { ItemManagementScreen, EmployeeTerminalScreen, AdminDashboard });
