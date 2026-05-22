// Reports: Z-Report, Daily Sales, Employee-wise

const SparkBar = ({ data, height = 60, color = "var(--navy-800)" }) => {
  const max = Math.max(...data);
  return (
    <div style={{ display: 'flex', alignItems: 'flex-end', gap: 3, height }}>
      {data.map((v, i) => (
        <div key={i} style={{
          flex: 1, height: `${(v / max) * 100}%`, background: color, borderRadius: 2,
          minHeight: 2, opacity: 0.85 - (i / data.length) * 0.3 + 0.3,
        }}/>
      ))}
    </div>
  );
};

const ZReportScreen = () => (
  <div style={{ padding: 28, height: '100%', overflow: 'auto', background: 'var(--surface)' }}>
    <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: 18 }}>
      <div>
        <div style={{ fontSize: 11.5, color: 'var(--ink-400)', letterSpacing: '.08em', textTransform: 'uppercase', fontWeight: 500 }}>End-of-day report</div>
        <h1 style={{ margin: '4px 0 0', fontSize: 26, fontWeight: 600, letterSpacing: '-.02em' }}>Z-Report · POS-01 · 19 May 2026</h1>
        <div style={{ fontSize: 13, color: 'var(--ink-500)', marginTop: 4 }}>Shift S-4218 · Adeel · Opened 09:00 · Closed 17:30</div>
      </div>
      <div style={{ display: 'flex', gap: 10 }}>
        <Select options={["Per-shift","Per-terminal","Per-business-day"]} value="Per-shift" onChange={()=>{}}/>
        <Btn variant="default" icon="download">Export</Btn>
        <Btn variant="primary" icon="print">Print Z-Report</Btn>
      </div>
    </div>

    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12, marginBottom: 20 }}>
      <Stat label="Gross sales" value="PKR 86,580" delta="+12.4% vs avg" deltaTone="success"/>
      <Stat label="Net sales" value="PKR 82,370" sub="after returns & discounts"/>
      <Stat label="Tax collected" value="PKR 4,210"/>
      <Stat label="Orders" value="184" sub="2 voided · 4 held"/>
    </div>

    <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: 20 }}>
      <Card padding={22}>
        <SectionTitle action={<span style={{ fontSize: 12, color: 'var(--ink-500)' }}>30-min buckets</span>}>Sales by hour</SectionTitle>
        <SparkBar data={[12, 28, 45, 62, 78, 88, 95, 110, 88, 72, 96, 130, 142, 96, 64, 48, 30]} height={140} color="var(--navy-800)"/>
        <div className="num" style={{ display: 'flex', justifyContent: 'space-between', fontSize: 11, color: 'var(--ink-400)', marginTop: 8 }}>
          <span>09:00</span><span>12:00</span><span>15:00</span><span>17:30</span>
        </div>
      </Card>

      <Card padding={22}>
        <SectionTitle>Reconciliation</SectionTitle>
        <KV k="Opening cash" v="PKR 15,000" mono/>
        <KV k="Cash sales" v="+ PKR 38,200" mono color="var(--green-700)"/>
        <KV k="Refunds (cash)" v="− PKR 1,200" mono color="var(--red-700)"/>
        <KV k="Payouts" v="− PKR 800" mono color="var(--red-700)"/>
        <KV k="Drawer → Vault" v="− PKR 10,000" mono/>
        <KV k="Expected cash" v="PKR 41,200" mono strong divider/>
        <KV k="Counted cash" v="PKR 41,200" mono color="var(--green-700)"/>
        <div style={{ marginTop: 14, padding: 12, background: 'var(--green-50)', borderRadius: 10, display: 'flex', alignItems: 'center', gap: 10 }}>
          <Icon name="check" size={18} color="var(--green-700)"/>
          <div style={{ fontSize: 12.5, color: 'var(--green-700)', fontWeight: 500 }}>Reconciled · variance PKR 0</div>
        </div>
      </Card>
    </div>

    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 20, marginTop: 20 }}>
      <Card padding={22}>
        <SectionTitle>Tender mix</SectionTitle>
        {[
          { k: "Cash",   v: 38200, c: "var(--green-600)", pct: 44.1 },
          { k: "Card",   v: 42150, c: "var(--blue-600)",  pct: 48.7 },
          { k: "Wallet", v: 4980,  c: "var(--amber-600)", pct: 5.8 },
          { k: "Voucher",v: 1250,  c: "var(--ink-400)",   pct: 1.4 },
        ].map(t => (
          <div key={t.k} style={{ marginBottom: 12 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12.5, marginBottom: 4 }}>
              <span>{t.k}</span>
              <span className="num">{fmtT(t.v)} · {t.pct}%</span>
            </div>
            <div style={{ height: 8, background: 'var(--surface-2)', borderRadius: 99 }}>
              <div style={{ height: '100%', width: `${t.pct}%`, background: t.c, borderRadius: 99 }}/>
            </div>
          </div>
        ))}
      </Card>

      <Card padding={22}>
        <SectionTitle>Top items</SectionTitle>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          {[
            { n: "Rice 5kg", q: 32, v: 59200 },
            { n: "Milk 1L", q: 96, v: 26880 },
            { n: "Cooking Oil 3L", q: 18, v: 29700 },
            { n: "Cola 1.5L", q: 64, v: 14080 },
            { n: "Tea Pack 500g", q: 12, v: 11400 },
          ].map((i, idx) => (
            <div key={idx} style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13, padding: '4px 0' }}>
              <div>
                <div style={{ fontWeight: 500 }}>{i.n}</div>
                <div className="num" style={{ fontSize: 11.5, color: 'var(--ink-500)' }}>{i.q} sold</div>
              </div>
              <span className="num" style={{ fontWeight: 600 }}>{fmtT(i.v)}</span>
            </div>
          ))}
        </div>
      </Card>

      <Card padding={22}>
        <SectionTitle>Audit events</SectionTitle>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          {[
            { t: "Voids", n: 2, tone: "danger", who: "by Adeel · approved Fahad" },
            { t: "Refunds", n: 1, tone: "amber", who: "by Adeel · approved Fahad" },
            { t: "Manager overrides", n: 4, tone: "amber", who: "by Fahad" },
            { t: "Price overrides", n: 0, tone: "neutral", who: "" },
            { t: "Failed prints", n: 0, tone: "neutral", who: "" },
            { t: "Failed sync chunks", n: 0, tone: "neutral", who: "" },
          ].map((e, i) => (
            <div key={i} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <div>
                <div style={{ fontSize: 13, fontWeight: 500 }}>{e.t}</div>
                {e.who && <div style={{ fontSize: 11.5, color: 'var(--ink-500)' }}>{e.who}</div>}
              </div>
              <Badge tone={e.tone}>{e.n}</Badge>
            </div>
          ))}
        </div>
      </Card>
    </div>
  </div>
);

const DailySalesReport = () => (
  <div style={{ padding: 28, height: '100%', overflow: 'auto', background: 'var(--surface)' }}>
    <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: 18 }}>
      <div>
        <div style={{ fontSize: 11.5, color: 'var(--ink-400)', letterSpacing: '.08em', textTransform: 'uppercase' }}>Reporting</div>
        <h1 style={{ margin: '4px 0 0', fontSize: 26, fontWeight: 600, letterSpacing: '-.02em' }}>Daily sales</h1>
      </div>
      <div style={{ display: 'flex', gap: 10 }}>
        <Select options={["All locations","Main Branch","City Branch","Airport Branch"]} value="All locations" onChange={()=>{}}/>
        <Select options={["This week","Today","Yesterday","Last 7 days","Last 30 days","Custom range"]} value="Last 7 days" onChange={()=>{}}/>
        <Btn variant="default" icon="filter">Filters</Btn>
        <Btn variant="default" icon="download">Export CSV</Btn>
        <Btn variant="primary" icon="print">Print</Btn>
      </div>
    </div>

    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12, marginBottom: 20 }}>
      <Stat label="Gross sales" value="PKR 6,284,210" delta="+8.2%" deltaTone="success" sub="vs prior 7 days"/>
      <Stat label="Net sales" value="PKR 5,962,180"/>
      <Stat label="Orders" value="3,842" sub="avg basket PKR 1,635"/>
      <Stat label="Refunds" value="PKR 86,420" delta="−12.4%" deltaTone="success" sub="68 refund lines"/>
    </div>

    <Card padding={22} style={{ marginBottom: 20 }}>
      <SectionTitle action={
        <div style={{ display: 'flex', gap: 8, fontSize: 12 }}>
          <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}><Dot tone="blue"/> Main Branch</span>
          <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}><Dot tone="success"/> City Branch</span>
          <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}><Dot tone="amber"/> Airport Branch</span>
        </div>
      }>Sales trend</SectionTitle>
      <div style={{ height: 220, position: 'relative' }}>
        {/* Simple multi-line chart */}
        <svg viewBox="0 0 700 220" style={{ width: '100%', height: '100%' }} preserveAspectRatio="none">
          {[40, 80, 120, 160, 200].map(y => <line key={y} x1="0" y1={y} x2="700" y2={y} stroke="var(--line)" strokeDasharray="2 4"/>)}
          {/* Main */}
          <path d="M 0 140 L 100 110 L 200 130 L 300 80 L 400 60 L 500 90 L 600 70 L 700 50" stroke="var(--blue-600)" strokeWidth="2.5" fill="none"/>
          <path d="M 0 140 L 100 110 L 200 130 L 300 80 L 400 60 L 500 90 L 600 70 L 700 50 L 700 220 L 0 220 Z" fill="var(--blue-600)" opacity="0.08"/>
          {/* City */}
          <path d="M 0 170 L 100 150 L 200 160 L 300 130 L 400 110 L 500 140 L 600 120 L 700 100" stroke="var(--green-600)" strokeWidth="2.5" fill="none"/>
          {/* Airport */}
          <path d="M 0 195 L 100 190 L 200 185 L 300 170 L 400 175 L 500 165 L 600 175 L 700 160" stroke="var(--amber-600)" strokeWidth="2.5" fill="none"/>
        </svg>
        <div style={{ position: 'absolute', bottom: -22, left: 0, right: 0, display: 'flex', justifyContent: 'space-between', fontSize: 11, color: 'var(--ink-400)' }}>
          {["Mon 13","Tue 14","Wed 15","Thu 16","Fri 17","Sat 18","Sun 19"].map(d => <span key={d}>{d}</span>)}
        </div>
      </div>
    </Card>

    <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: 20 }}>
      <Card padding={0}>
        <div style={{ padding: '14px 18px', borderBottom: '1px solid var(--line)', fontSize: 14, fontWeight: 600 }}>By location</div>
        <Table
          columns={[
            { key: "loc", label: "Location" },
            { key: "ord", label: "Orders", align: 'right', mono: true },
            { key: "gross", label: "Gross", align: 'right', mono: true },
            { key: "net", label: "Net", align: 'right', mono: true },
            { key: "tax", label: "Tax", align: 'right', mono: true },
            { key: "ref", label: "Refunds", align: 'right', mono: true },
            { key: "basket", label: "Avg basket", align: 'right', mono: true },
            { key: "trend", label: "vs prior", align: 'right', render: r => <Badge tone={r.trend > 0 ? "success" : "danger"} dot>{r.trend > 0 ? "+" : ""}{r.trend}%</Badge> },
          ]}
          rows={[
            { loc: "Main Branch",    ord: "1,842", gross: "PKR 3,184,210", net: "PKR 3,018,400", tax: "PKR 152,800", ref: "PKR 42,600", basket: "PKR 1,728", trend: 12.4 },
            { loc: "City Branch",    ord: "1,420", gross: "PKR 2,256,500", net: "PKR 2,118,720", tax: "PKR 118,200", ref: "PKR 28,400", basket: "PKR 1,589", trend: 6.2 },
            { loc: "Airport Branch", ord: "580",   gross: "PKR 843,500",   net: "PKR 825,060",   tax: "PKR 42,400",  ref: "PKR 15,420", basket: "PKR 1,454", trend: -2.8 },
          ]}
        />
      </Card>

      <Card padding={22}>
        <SectionTitle>Payment mix</SectionTitle>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', marginBottom: 18 }}>
          <svg width="180" height="180" viewBox="0 0 180 180">
            {/* Donut */}
            <circle cx="90" cy="90" r="64" stroke="var(--surface-2)" strokeWidth="22" fill="none"/>
            <circle cx="90" cy="90" r="64" stroke="var(--blue-600)" strokeWidth="22" fill="none"
              strokeDasharray={`${0.51 * 402} 402`} transform="rotate(-90 90 90)"/>
            <circle cx="90" cy="90" r="64" stroke="var(--green-600)" strokeWidth="22" fill="none"
              strokeDasharray={`${0.34 * 402} 402`} strokeDashoffset={`${-0.51 * 402}`} transform="rotate(-90 90 90)"/>
            <circle cx="90" cy="90" r="64" stroke="var(--amber-600)" strokeWidth="22" fill="none"
              strokeDasharray={`${0.10 * 402} 402`} strokeDashoffset={`${-0.85 * 402}`} transform="rotate(-90 90 90)"/>
            <text x="90" y="86" textAnchor="middle" fontSize="22" fontWeight="600" fontFamily="IBM Plex Mono">PKR</text>
            <text x="90" y="106" textAnchor="middle" fontSize="14" fontFamily="IBM Plex Mono" fill="var(--ink-500)">6.28M</text>
          </svg>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8, fontSize: 12.5 }}>
          {[{c:"var(--blue-600)",k:"Card",v:"51%"},{c:"var(--green-600)",k:"Cash",v:"34%"},{c:"var(--amber-600)",k:"Wallet",v:"10%"},{c:"var(--ink-400)",k:"Voucher",v:"5%"}].map(r => (
            <div key={r.k} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
                <span style={{ width: 8, height: 8, background: r.c, borderRadius: 99 }}/> {r.k}
              </span>
              <span className="num" style={{ fontWeight: 500 }}>{r.v}</span>
            </div>
          ))}
        </div>
      </Card>
    </div>
  </div>
);

const EmployeeReport = () => {
  const rows = [
    { id: "E1042", name: "Adeel",  loc: "Main Branch", shifts: 6, ord: 942, sales: 1284600, voids: 4, ovr: 2, avg: 1364, score: 96 },
    { id: "E1044", name: "Sana",   loc: "City Branch",  shifts: 5, ord: 712, sales: 982400,  voids: 2, ovr: 1, avg: 1380, score: 98 },
    { id: "E1045", name: "Bilal",  loc: "Main Branch", shifts: 6, ord: 624, sales: 728400,  voids: 6, ovr: 5, avg: 1168, score: 84 },
    { id: "E1048", name: "Usman",  loc: "Airport Branch", shifts: 4, ord: 312, sales: 415200,  voids: 3, ovr: 3, avg: 1332, score: 88 },
    { id: "E1051", name: "Ayesha", loc: "Main Branch", shifts: 6, ord: 588, sales: 814200,  voids: 1, ovr: 0, avg: 1385, score: 99 },
    { id: "E1053", name: "Imran",  loc: "City Branch",  shifts: 5, ord: 491, sales: 612800,  voids: 3, ovr: 2, avg: 1248, score: 91 },
  ];
  return (
    <div style={{ padding: 28, height: '100%', overflow: 'auto', background: 'var(--surface)' }}>
      <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: 18 }}>
        <div>
          <div style={{ fontSize: 11.5, color: 'var(--ink-400)', letterSpacing: '.08em', textTransform: 'uppercase' }}>Reporting</div>
          <h1 style={{ margin: '4px 0 0', fontSize: 26, fontWeight: 600, letterSpacing: '-.02em' }}>Employee performance</h1>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          <Select options={["All locations","Main Branch","City Branch","Airport Branch"]} value="All locations" onChange={()=>{}}/>
          <Select options={["Last 7 days","Today","This month"]} value="Last 7 days" onChange={()=>{}}/>
          <Btn variant="default" icon="download">Export</Btn>
        </div>
      </div>

      <Card padding={0}>
        <div style={{ padding: '14px 18px', borderBottom: '1px solid var(--line)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div style={{ fontSize: 14, fontWeight: 600 }}>{rows.length} cashiers · last 7 days</div>
          <div style={{ display: 'flex', gap: 8 }}>
            <Input icon="search" placeholder="Search cashier" style={{ height: 34 }}/>
            <Btn variant="default" size="sm" icon="filter">Filter</Btn>
          </div>
        </div>
        <Table
          columns={[
            { key: "name", label: "Cashier", render: r => (
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <div style={{ width: 32, height: 32, borderRadius: 99, background: 'var(--navy-800)', color: '#fff', display: 'grid', placeItems: 'center', fontSize: 12, fontWeight: 600 }}>
                  {r.name.slice(0, 2).toUpperCase()}
                </div>
                <div>
                  <div style={{ fontWeight: 500 }}>{r.name}</div>
                  <div className="num" style={{ fontSize: 11.5, color: 'var(--ink-500)' }}>{r.id} · {r.loc}</div>
                </div>
              </div>
            )},
            { key: "shifts", label: "Shifts", align: 'right', mono: true },
            { key: "ord", label: "Orders", align: 'right', mono: true },
            { key: "sales", label: "Net sales", align: 'right', mono: true, render: r => fmtT(r.sales) },
            { key: "avg", label: "Avg basket", align: 'right', mono: true, render: r => fmtT(r.avg) },
            { key: "voids", label: "Voids", align: 'right', mono: true, render: r => <span style={{ color: r.voids > 4 ? 'var(--amber-700)' : 'var(--ink-600)' }}>{r.voids}</span> },
            { key: "ovr", label: "Mgr overrides", align: 'right', mono: true, render: r => <span style={{ color: r.ovr > 3 ? 'var(--amber-700)' : 'var(--ink-600)' }}>{r.ovr}</span> },
            { key: "score", label: "Score", align: 'right', render: r => (
              <div style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
                <div style={{ width: 60, height: 6, background: 'var(--surface-2)', borderRadius: 99 }}>
                  <div style={{ width: `${r.score}%`, height: '100%', background: r.score >= 95 ? 'var(--green-600)' : r.score >= 85 ? 'var(--amber-600)' : 'var(--red-600)', borderRadius: 99 }}/>
                </div>
                <span className="num" style={{ fontWeight: 500 }}>{r.score}</span>
              </div>
            )},
          ]}
          rows={rows}
        />
      </Card>
    </div>
  );
};

Object.assign(window, { ZReportScreen, DailySalesReport, EmployeeReport });
