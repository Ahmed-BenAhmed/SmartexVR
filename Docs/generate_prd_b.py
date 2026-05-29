"""
Generates the PRD for Projet B — SmartTwin-AR as a professional PDF.
Run: python generate_prd_b.py
"""

from reportlab.lib.pagesizes import A4
from reportlab.lib import colors
from reportlab.lib.units import cm
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_JUSTIFY
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle,
    HRFlowable, KeepTogether
)

# ── Output path ───────────────────────────────────────────────────────────────
OUTPUT = r"D:\ingenerie-et-maquette-numerique\UNITY\My Project\SmartexVR\Docs\PRD_Projet_B_SmartTwin_AR.pdf"

# ── Brand colours ─────────────────────────────────────────────────────────────
DARK_NAVY   = colors.HexColor("#0D1B2A")
TEAL        = colors.HexColor("#0E9AA7")
TEAL_LIGHT  = colors.HexColor("#D6F4F7")
ORANGE      = colors.HexColor("#F4A261")
ORANGE_LITE = colors.HexColor("#FEF0E6")
WHITE       = colors.white
GREY_TEXT   = colors.HexColor("#4A4A4A")
GREY_BG     = colors.HexColor("#F5F7FA")
BORDER      = colors.HexColor("#CBD5E1")

# ── Document ──────────────────────────────────────────────────────────────────
doc = SimpleDocTemplate(
    OUTPUT, pagesize=A4,
    leftMargin=2*cm, rightMargin=2*cm,
    topMargin=2.5*cm, bottomMargin=2.5*cm,
    title="PRD Projet B — SmartTwin-AR",
    author="SmartexVR Team",
    subject="Product Requirements Document"
)

W = A4[0] - 4*cm   # usable width

# ── Styles ────────────────────────────────────────────────────────────────────
base = getSampleStyleSheet()

def S(name, **kw):
    return ParagraphStyle(name, **kw)

styles = {
    "cover_title": S("cover_title",
        fontName="Helvetica-Bold", fontSize=26, textColor=WHITE,
        leading=32, alignment=TA_CENTER, spaceAfter=6),

    "cover_sub": S("cover_sub",
        fontName="Helvetica", fontSize=13, textColor=TEAL_LIGHT,
        leading=18, alignment=TA_CENTER, spaceAfter=4),

    "cover_meta": S("cover_meta",
        fontName="Helvetica", fontSize=10, textColor=colors.HexColor("#A0B4C8"),
        alignment=TA_CENTER, spaceAfter=2),

    "section_head": S("section_head",
        fontName="Helvetica-Bold", fontSize=12, textColor=WHITE,
        leading=16, spaceBefore=14, spaceAfter=0,
        leftIndent=0, backColor=TEAL,
        borderPad=(6, 8, 6, 8)),

    "body": S("body",
        fontName="Helvetica", fontSize=10, textColor=GREY_TEXT,
        leading=15, alignment=TA_JUSTIFY, spaceAfter=6),

    "bullet": S("bullet",
        fontName="Helvetica", fontSize=10, textColor=GREY_TEXT,
        leading=14, leftIndent=14, firstLineIndent=-10, spaceAfter=3),

    "label": S("label",
        fontName="Helvetica-Bold", fontSize=10, textColor=DARK_NAVY,
        leading=14, spaceAfter=2),

    "badge": S("badge",
        fontName="Helvetica-Bold", fontSize=9, textColor=WHITE,
        alignment=TA_CENTER),

    "table_head": S("table_head",
        fontName="Helvetica-Bold", fontSize=9, textColor=WHITE,
        alignment=TA_CENTER, leading=12),

    "table_cell": S("table_cell",
        fontName="Helvetica", fontSize=9, textColor=GREY_TEXT,
        leading=12, alignment=TA_LEFT),

    "table_cell_c": S("table_cell_c",
        fontName="Helvetica", fontSize=9, textColor=GREY_TEXT,
        leading=12, alignment=TA_CENTER),

    "footer_note": S("footer_note",
        fontName="Helvetica-Oblique", fontSize=8,
        textColor=colors.HexColor("#94A3B8"),
        alignment=TA_CENTER),
}

# ── Helper: section header ────────────────────────────────────────────────────
def section(title, icon=""):
    data = [[Paragraph(f"{icon}  {title}" if icon else title, styles["section_head"])]]
    t = Table(data, colWidths=[W])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0,0), (-1,-1), TEAL),
        ("TOPPADDING",    (0,0), (-1,-1), 7),
        ("BOTTOMPADDING", (0,0), (-1,-1), 7),
        ("LEFTPADDING",   (0,0), (-1,-1), 10),
        ("RIGHTPADDING",  (0,0), (-1,-1), 10),
        ("ROWBACKGROUNDS", (0,0), (-1,-1), [TEAL]),
    ]))
    return t

def bullet(text):
    return Paragraph(f"<bullet>&bull;</bullet> {text}", styles["bullet"])

def body(text):
    return Paragraph(text, styles["body"])

def label(text):
    return Paragraph(text, styles["label"])

def sp(h=6):
    return Spacer(1, h)

def hr():
    return HRFlowable(width=W, thickness=0.5, color=BORDER, spaceAfter=6, spaceBefore=6)

# ── Cover page ────────────────────────────────────────────────────────────────
def cover_block():
    # Dark navy banner
    cover_data = [[
        Paragraph("SmartTwin-AR", styles["cover_title"]),
    ],[
        Paragraph(
            "Inspection Terrain Assist\u00e9e par R\u00e9alit\u00e9 Augment\u00e9e et Agent Conversationnel<br/>"
            "Incarn\u00e9 dans un Jumeau Num\u00e9rique Industriel",
            styles["cover_sub"]),
    ],[
        Paragraph("Projet B &mdash; Niveau Interm\u00e9diaire &nbsp; \u2605\u2605", styles["cover_meta"]),
    ],[
        Paragraph("Product Requirements Document &nbsp;&bull;&nbsp; SmartexVR++ Platform", styles["cover_meta"]),
    ],[
        Paragraph("Universit\u00e9 &nbsp;&bull;&nbsp; 2025&ndash;2026 &nbsp;&bull;&nbsp; \u00c9quipe de 7", styles["cover_meta"]),
    ]]
    t = Table(cover_data, colWidths=[W])
    t.setStyle(TableStyle([
        ("BACKGROUND",    (0,0), (-1,-1), DARK_NAVY),
        ("TOPPADDING",    (0,0), (0,0),   24),
        ("BOTTOMPADDING", (0,-1),(-1,-1), 24),
        ("TOPPADDING",    (0,1), (-1,-1),  6),
        ("BOTTOMPADDING", (0,0), (-1,-2),  6),
        ("LEFTPADDING",   (0,0), (-1,-1), 20),
        ("RIGHTPADDING",  (0,0), (-1,-1), 20),
        ("ROUNDEDCORNERS", [8]),
    ]))
    return t

# ── Difficulty badge ──────────────────────────────────────────────────────────
def badge_row():
    items = [
        (TEAL,   "Niveau Interm\u00e9diaire"),
        (ORANGE, "5\u20137 semaines"),
        (colors.HexColor("#6366F1"), "7 membres"),
        (colors.HexColor("#10B981"), "\u00c9quipe AR + IA + Backend"),
    ]
    cells = []
    widths = []
    for col, txt in items:
        p = Paragraph(txt, ParagraphStyle("b2",
            fontName="Helvetica-Bold", fontSize=9,
            textColor=WHITE, alignment=TA_CENTER))
        cells.append(p)
        widths.append(W / len(items))

    t = Table([cells], colWidths=widths, rowHeights=[22])
    style = [
        ("TOPPADDING",    (0,0), (-1,-1), 4),
        ("BOTTOMPADDING", (0,0), (-1,-1), 4),
        ("LEFTPADDING",   (0,0), (-1,-1), 4),
        ("RIGHTPADDING",  (0,0), (-1,-1), 4),
    ]
    for i, (col, _) in enumerate(items):
        style.append(("BACKGROUND", (i,0), (i,0), col))
        style.append(("ROUNDEDCORNERS", [4]))
    t.setStyle(TableStyle(style))
    return t

# ── Team table ────────────────────────────────────────────────────────────────
def team_table():
    headers = ["Membre", "Module", "Technologies cl\u00e9s", "Livrable"]
    rows = [
        ["1", "AR Foundation Core\n+ QR Machine Recognition",
         "ARFoundation 6, ARTrackedImageManager,\nARAnchorManager, XRReferenceImageLibrary",
         "Scan QR -> overlay\nanch\u00e9 sur machine r\u00e9elle"],
        ["2", "Agent IA Incarn\u00e9\n(Drone IEIA)",
         "Unity AI Navigation 2, NavMeshAgent,\nParticleSystem, Animator",
         "Drone se d\u00e9place vers\nla machine \u00e0 risque"],
        ["3", "Interface Vocale\n(STT + TTS)",
         "Whisper STT (relay FastAPI),\nTTS audio clip, HTTP POST",
         "Question vocale ->\nr\u00e9ponse AR en 2s"],
        ["4", "AR Data Overlay UI",
         "Billboard shaders, LateUpdate LookAt,\nTextMeshPro, ARAnchor",
         "Panneaux AR flottants\nau-dessus des machines"],
        ["5", "Backend Voice + IEIA\nExtensions",
         "FastAPI, Whisper, OpenRouter,\nWebSocket alerts",
         "Endpoint /voice/query\nfonctionnel"],
        ["6", "Maintenance Workflow\n+ Behavior Logging",
         "XR Interaction Toolkit, InfluxDB\nbatch write 60Hz",
         "Guide \u00e9tape par \u00e9tape\n+ log cin\u00e9matique"],
        ["7", "DevOps + Int\u00e9gration\n+ Build Android/iOS",
         "Unity Cloud Build, Android ARCore,\niOS ARKit, platform switching",
         "APK Android\nd\u00e9ployable"],
    ]

    col_w = [1.0*cm, 3.8*cm, 5.2*cm, 3.8*cm]
    table_data = [
        [Paragraph(h, styles["table_head"]) for h in headers]
    ] + [
        [Paragraph(c.replace("\n", "<br/>"), styles["table_cell_c"] if i == 0 else styles["table_cell"])
         for i, c in enumerate(row)]
        for row in rows
    ]

    t = Table(table_data, colWidths=col_w, repeatRows=1)
    ts = TableStyle([
        # Header
        ("BACKGROUND",    (0,0), (-1,0),  DARK_NAVY),
        ("TEXTCOLOR",     (0,0), (-1,0),  WHITE),
        ("TOPPADDING",    (0,0), (-1,0),  7),
        ("BOTTOMPADDING", (0,0), (-1,0),  7),
        # Alternating rows
        ("ROWBACKGROUNDS", (0,1), (-1,-1), [WHITE, GREY_BG]),
        ("TOPPADDING",    (0,1), (-1,-1),  5),
        ("BOTTOMPADDING", (0,1), (-1,-1),  5),
        ("LEFTPADDING",   (0,0), (-1,-1),  6),
        ("RIGHTPADDING",  (0,0), (-1,-1),  6),
        # Grid
        ("GRID",          (0,0), (-1,-1),  0.4, BORDER),
        ("VALIGN",        (0,0), (-1,-1), "MIDDLE"),
    ])
    t.setStyle(ts)
    return t

# ── Architecture diagram (text-based) ────────────────────────────────────────
def arch_table():
    rows = [
        ["Mobile AR (Technicien)",  "<->  relay FastAPI  <->",  "Desktop 3D Twin (Superviseur)"],
        ["ARFoundation 6\nQR -> device_id\nOverlay AR\nDrone IEIA\nVoice STT",
         "FastAPI\n/snapshot\n/voice/query\n/maintenance\n/ws/alerts",
         "Unity URP\nHUD KPIs\nIEIA Chat\nWhat-if CBAM\nVue isom\u00e9trique"],
    ]
    col_w = [(W - 1.6*cm) / 3, 1.6*cm + (W - 1.6*cm)/3 * 0, (W - 1.6*cm)/3]
    col_w = [W*0.36, W*0.28, W*0.36]

    tdata = [
        [Paragraph(rows[0][0], ParagraphStyle("ah", fontName="Helvetica-Bold",
            fontSize=9, textColor=WHITE, alignment=TA_CENTER)),
         Paragraph(rows[0][1], ParagraphStyle("ah2", fontName="Helvetica-Bold",
            fontSize=8, textColor=TEAL_LIGHT, alignment=TA_CENTER)),
         Paragraph(rows[0][2], ParagraphStyle("ah3", fontName="Helvetica-Bold",
            fontSize=9, textColor=WHITE, alignment=TA_CENTER))],
        [Paragraph(rows[1][0].replace("\n","<br/>"), ParagraphStyle("ac",
            fontName="Helvetica", fontSize=8.5, textColor=GREY_TEXT,
            alignment=TA_CENTER, leading=13)),
         Paragraph(rows[1][1].replace("\n","<br/>"), ParagraphStyle("ac2",
            fontName="Helvetica", fontSize=8, textColor=GREY_TEXT,
            alignment=TA_CENTER, leading=12)),
         Paragraph(rows[1][2].replace("\n","<br/>"), ParagraphStyle("ac3",
            fontName="Helvetica", fontSize=8.5, textColor=GREY_TEXT,
            alignment=TA_CENTER, leading=13))],
    ]
    t = Table(tdata, colWidths=col_w)
    t.setStyle(TableStyle([
        ("BACKGROUND",    (0,0), (0,-1), DARK_NAVY),
        ("BACKGROUND",    (1,0), (1,-1), colors.HexColor("#1E3A4A")),
        ("BACKGROUND",    (2,0), (2,-1), DARK_NAVY),
        ("TOPPADDING",    (0,0), (-1,-1), 8),
        ("BOTTOMPADDING", (0,0), (-1,-1), 8),
        ("LEFTPADDING",   (0,0), (-1,-1), 8),
        ("RIGHTPADDING",  (0,0), (-1,-1), 8),
        ("GRID",          (0,0), (-1,-1), 0.5, TEAL),
        ("VALIGN",        (0,0), (-1,-1), "TOP"),
        ("ROUNDEDCORNERS", [6]),
    ]))
    return t

# ── Scientific contributions table ───────────────────────────────────────────
def sci_table():
    rows = [
        ["Utilisabilit\u00e9 AR industrielle",
         "Comparaison inspection AR mobile vs tablette/papier\n(temps, erreurs, satisfaction)"],
        ["Confiance envers l'IA incarn\u00e9e",
         "Impact de la pr\u00e9sence spatiale du drone IEIA\nsur l'adoption des recommandations"],
        ["Communication vocale H-M",
         "Efficacit\u00e9 STT en environnement industriel\nbruyant (SNR variable)"],
        ["Tra\u00e7abilit\u00e9 CBAM par AR",
         "Scan machine -> exposition CO<sub>2</sub> instantan\u00e9e\n(validation r\u00e9glementation 2026)"],
    ]
    col_w = [W * 0.32, W * 0.68]
    tdata = [[Paragraph(r[0], styles["table_head"]),
              Paragraph(r[1].replace("\n","<br/>"), styles["table_cell"])]
             for r in rows]
    t = Table(tdata, colWidths=col_w)
    t.setStyle(TableStyle([
        ("ROWBACKGROUNDS", (0,0), (-1,-1), [TEAL_LIGHT, WHITE]),
        ("BACKGROUND",     (0,0), (0,-1),  TEAL),
        ("TEXTCOLOR",      (0,0), (0,-1),  WHITE),
        ("TOPPADDING",    (0,0), (-1,-1), 6),
        ("BOTTOMPADDING", (0,0), (-1,-1), 6),
        ("LEFTPADDING",   (0,0), (-1,-1), 8),
        ("RIGHTPADDING",  (0,0), (-1,-1), 8),
        ("GRID",          (0,0), (-1,-1), 0.4, BORDER),
        ("VALIGN",        (0,0), (-1,-1), "MIDDLE"),
    ]))
    return t

# ── Packages table ────────────────────────────────────────────────────────────
def pkg_table():
    rows = [
        ["com.unity.xr.arfoundation",        "6.0.x",  "AR session, plane detection, anchors"],
        ["com.unity.xr.arcore",              "6.0.x",  "Android ARCore backend"],
        ["com.unity.xr.arkit",               "6.0.x",  "iOS ARKit backend"],
        ["com.unity.xr.management",          "4.4.x",  "XR plugin lifecycle"],
        ["com.unity.xr.interaction.toolkit", "3.x.x",  "Interaction AR/VR unifi\u00e9e"],
        ["com.unity.ai.navigation",          "2.0.x",  "NavMesh drone IEIA (d\u00e9j\u00e0 pr\u00e9sent)"],
        ["com.unity.inputsystem",            "1.19.0", "New Input System (d\u00e9j\u00e0 pr\u00e9sent)"],
    ]
    col_w = [W*0.38, W*0.14, W*0.48]
    tdata = [
        [Paragraph("Package Unity", styles["table_head"]),
         Paragraph("Version", styles["table_head"]),
         Paragraph("R\u00f4le", styles["table_head"])]
    ] + [
        [Paragraph(r[0], ParagraphStyle("mono", fontName="Courier", fontSize=8,
                   textColor=DARK_NAVY, leading=11)),
         Paragraph(r[1], styles["table_cell_c"]),
         Paragraph(r[2], styles["table_cell"])]
        for r in rows
    ]
    t = Table(tdata, colWidths=col_w, repeatRows=1)
    t.setStyle(TableStyle([
        ("BACKGROUND",    (0,0), (-1,0),  DARK_NAVY),
        ("ROWBACKGROUNDS",(0,1), (-1,-1), [WHITE, GREY_BG]),
        ("GRID",          (0,0), (-1,-1), 0.4, BORDER),
        ("TOPPADDING",    (0,0), (-1,-1), 5),
        ("BOTTOMPADDING", (0,0), (-1,-1), 5),
        ("LEFTPADDING",   (0,0), (-1,-1), 7),
        ("RIGHTPADDING",  (0,0), (-1,-1), 7),
        ("VALIGN",        (0,0), (-1,-1), "MIDDLE"),
    ]))
    return t

# ── Callout box ───────────────────────────────────────────────────────────────
def callout(text, bg=ORANGE_LITE, border=ORANGE):
    p = Paragraph(text, ParagraphStyle("co",
        fontName="Helvetica", fontSize=9.5, textColor=DARK_NAVY,
        leading=14, alignment=TA_JUSTIFY))
    t = Table([[p]], colWidths=[W])
    t.setStyle(TableStyle([
        ("BACKGROUND",    (0,0), (-1,-1), bg),
        ("LEFTPADDING",   (0,0), (-1,-1), 12),
        ("RIGHTPADDING",  (0,0), (-1,-1), 12),
        ("TOPPADDING",    (0,0), (-1,-1), 10),
        ("BOTTOMPADDING", (0,0), (-1,-1), 10),
        ("LINEAFTER",     (0,0), (0,-1), 3, border),
        ("LINEBEFORE",    (0,0), (0,-1), 3, border),
    ]))
    return t

# ── Build story ───────────────────────────────────────────────────────────────
story = []

# Cover
story.append(cover_block())
story.append(sp(10))
story.append(badge_row())
story.append(sp(16))

# ── 1. Titre & Concept ────────────────────────────────────────────────────────
story.append(section("1. Titre du Projet", "\u25a0"))
story.append(sp(8))
story.append(callout(
    "<b>SmartTwin-AR</b> &mdash; Inspection Terrain Assist\u00e9e par R\u00e9alit\u00e9 Augment\u00e9e "
    "et Agent Conversationnel Incarn\u00e9 dans un Jumeau Num\u00e9rique Industriel Textile",
    bg=TEAL_LIGHT, border=TEAL
))
story.append(sp(12))

# ── 2. Description du concept ─────────────────────────────────────────────────
story.append(section("2. Description du Concept", "\u25a0"))
story.append(sp(8))
story.append(body(
    "Le syst\u00e8me op\u00e8re sur <b>deux interfaces simultan\u00e9es et compl\u00e9mentaires</b>. "
    "Sur Desktop, le superviseur dispose du jumeau 3D complet de l\u2019usine textile TNG-01 (Tanger), "
    "affichant en temps r\u00e9el les donn\u00e9es des 8 m\u00e9tiers \u00e0 tisser (puissance, vibration, "
    "temp\u00e9rature de cuve, score de sant\u00e9, exposition CBAM). "
    "Sur mobile Android ou iOS, le technicien de terrain pointe sa cam\u00e9ra vers un <b>QR code "
    "fix\u00e9 physiquement sur chaque m\u00e9tier</b> : un panneau AR ancr\u00e9 dans l\u2019espace "
    "r\u00e9el surgit au-dessus de la machine avec l\u2019ensemble de ses capteurs en temps r\u00e9el. "
    "Une bague de sant\u00e9 color\u00e9e (vert / orange / rouge) pulse autour du socle physique de la machine."
))
story.append(sp(4))
story.append(body(
    "L\u2019agent <b>IEIA est incarn\u00e9 sous la forme d\u2019un drone virtuel</b> visible \u00e0 la fois "
    "dans la vue AR du technicien et dans le jumeau Desktop du superviseur. Le drone se d\u00e9place "
    "autonomement vers la machine pr\u00e9sentant le risque de d\u00e9faillance le plus \u00e9lev\u00e9, "
    "y ancre un hologramme d\u2019alerte, et r\u00e9pond aux <b>questions vocales</b> du technicien "
    "via reconnaissance automatique de la parole en temps r\u00e9el."
))
story.append(sp(12))

# ── 3. Architecture ───────────────────────────────────────────────────────────
story.append(section("3. Architecture Syst\u00e8me", "\u25a0"))
story.append(sp(8))
story.append(arch_table())
story.append(sp(4))
story.append(Paragraph(
    "<i>Les deux clients partagent le m\u00eame relay FastAPI &mdash; aucun Netcode requis \u00e0 ce niveau.</i>",
    styles["footer_note"]))
story.append(sp(12))

# ── 4. Rôle IA / Big Data ─────────────────────────────────────────────────────
story.append(section("4. R\u00f4le de l\u2019IA et de la Big Data", "\u25a0"))
story.append(sp(8))

story.append(label("4.1 Flux temps r\u00e9el (InfluxDB)"))
story.append(bullet("D\u00e9tection d\u2019anomalie par z-score glissant sur fen\u00eatre 30 min par machine"))
story.append(bullet("Seuils dynamiques ajust\u00e9s par l\u2019historique de chaque loom (pas de faux positifs li\u00e9s au d\u00e9marrage \u00e0 froid)"))
story.append(bullet("Push WebSocket vers les deux clients d\u00e8s qu\u2019une anomalie est d\u00e9tect\u00e9e"))
story.append(sp(6))

story.append(label("4.2 Agent IEIA (LLM orchestr\u00e9 via OpenRouter)"))
story.append(bullet("Ing\u00e8re le snapshot courant + l\u2019historique PostgreSQL (maintenance_logs, reliability_records)"))
story.append(bullet("G\u00e9n\u00e8re des sc\u00e9narios contrefactuels CBAM : \u00ab Si maintenance Loom 5 aujourd\u2019hui \u2192 \u00e9conomie 840 MAD/an \u00bb"))
story.append(bullet("Coefficient d\u2019usure roulement : 0,12 kWh/unit\u00e9 par unit\u00e9 d\u2019usure (mod\u00e8le causal Paper 4, EXP2)"))
story.append(sp(6))

story.append(label("4.3 Interface Vocale (Whisper STT)"))
story.append(bullet("Whisper ex\u00e9cut\u00e9 localement sur le relay FastAPI (endpoint /voice/query)"))
story.append(bullet("La question audio du technicien est transcrite, inject\u00e9e dans le contexte IEIA, la r\u00e9ponse synth\u00e9tis\u00e9e (TTS) est retourn\u00e9e en clip audio + texte AR"))
story.append(bullet("Latence cible : < 2,5 secondes bout-en-bout sur WiFi 5GHz d\u2019usine"))
story.append(sp(12))

# ── 5. Interaction collaborative ──────────────────────────────────────────────
story.append(section("5. Type d\u2019Interaction Collaborative", "\u25a0"))
story.append(sp(8))
story.append(body(
    "<b>Hybride Desktop + Mobile AR, asym\u00e9trique l\u00e9ger.</b> "
    "Les deux utilisateurs voient les m\u00eames donn\u00e9es (via le DataManager connect\u00e9 au m\u00eame relay), "
    "mais depuis des perspectives radicalement diff\u00e9rentes. "
    "Le superviseur dispose d\u2019une omniscience analytique (HUD global, historique, CBAM total) ; "
    "le technicien dispose d\u2019une immersion physique locale (ce qu\u2019il voit, la machine devant lui). "
    "Aucun Netcode n\u2019est requis : la synchronisation est assur\u00e9e par le relay FastAPI commun "
    "avec polling \u00e0 5 secondes et push WebSocket pour les alertes critiques."
))
story.append(sp(12))

# ── 6. Contributions scientifiques ───────────────────────────────────────────
story.append(section("6. Contributions Scientifiques Possibles", "\u25a0"))
story.append(sp(8))
story.append(sci_table())
story.append(sp(12))

# ── 7. Team ───────────────────────────────────────────────────────────────────
story.append(KeepTogether([
    section("7. R\u00e9partition de l\u2019\u00c9quipe (7 membres)", "\u25a0"),
    sp(8),
    team_table(),
]))
story.append(sp(12))

# ── 8. Packages ───────────────────────────────────────────────────────────────
story.append(section("8. Packages Unity \u00e0 Ajouter", "\u25a0"))
story.append(sp(8))
story.append(pkg_table())
story.append(sp(12))

# ── 9. Waves ──────────────────────────────────────────────────────────────────
story.append(section("9. Plan de Livraison (Waves)", "\u25a0"))
story.append(sp(8))

waves = [
    ["Wave 1\n(sem. 1\u20132)", "AR Foundation Setup",
     "ARSession op\u00e9rationnel, QR code reconnu, panneau AR ancr\u00e9 au-dessus machine"],
    ["Wave 2\n(sem. 3\u20134)", "Overlay Temps R\u00e9el",
     "Donn\u00e9es InfluxDB affich\u00e9es en AR, bague sant\u00e9 color\u00e9e, alerte WebSocket"],
    ["Wave 3\n(sem. 4\u20135)", "Drone IEIA Incarn\u00e9",
     "Drone se d\u00e9place vers machine \u00e0 risque, hologramme d\u2019alerte ancr\u00e9"],
    ["Wave 4\n(sem. 5\u20136)", "Interface Vocale",
     "Question vocale \u2192 Whisper \u2192 IEIA \u2192 TTS \u2192 r\u00e9ponse bulle AR"],
    ["Wave 5\n(sem. 6\u20137)", "Int\u00e9gration & Demo",
     "APK Android deploy\u00e9, Desktop + AR synchronis\u00e9s, pr\u00e9sentation usine"],
]

col_w2 = [W*0.18, W*0.22, W*0.60]
wdata = [
    [Paragraph("Phase", styles["table_head"]),
     Paragraph("Titre", styles["table_head"]),
     Paragraph("Livrable", styles["table_head"])]
] + [
    [Paragraph(r[0].replace("\n","<br/>"), styles["table_cell_c"]),
     Paragraph(r[1], ParagraphStyle("wt", fontName="Helvetica-Bold", fontSize=9,
               textColor=TEAL, leading=12)),
     Paragraph(r[2], styles["table_cell"])]
    for r in waves
]
wt = Table(wdata, colWidths=col_w2, repeatRows=1)
wt.setStyle(TableStyle([
    ("BACKGROUND",     (0,0), (-1,0),  DARK_NAVY),
    ("ROWBACKGROUNDS", (0,1), (-1,-1), [WHITE, GREY_BG]),
    ("GRID",           (0,0), (-1,-1), 0.4, BORDER),
    ("TOPPADDING",    (0,0), (-1,-1), 6),
    ("BOTTOMPADDING", (0,0), (-1,-1), 6),
    ("LEFTPADDING",   (0,0), (-1,-1), 7),
    ("RIGHTPADDING",  (0,0), (-1,-1), 7),
    ("VALIGN",        (0,0), (-1,-1), "MIDDLE"),
]))
story.append(wt)
story.append(sp(12))

# ── 10. Position within full vision ──────────────────────────────────────────
story.append(section("10. Position dans la Vision Globale SmartexVR++", "\u25a0"))
story.append(sp(8))

level_data = [
    [Paragraph("\u2b50 Projet A", ParagraphStyle("la",
        fontName="Helvetica-Bold", fontSize=9, textColor=WHITE, alignment=TA_CENTER)),
     Paragraph("\u2b50\u2b50 Projet B <b>(ce PRD)</b>", ParagraphStyle("lb",
        fontName="Helvetica-Bold", fontSize=9, textColor=DARK_NAVY, alignment=TA_CENTER)),
     Paragraph("\u2b50\u2b50\u2b50 Projet C", ParagraphStyle("lc",
        fontName="Helvetica-Bold", fontSize=9, textColor=WHITE, alignment=TA_CENTER))],
    [Paragraph("Jumeau 3D Desktop<br/>+ Chat IEIA<br/>Mono-utilisateur", styles["table_cell_c"]),
     Paragraph("<b>Jumeau + AR Mobile<br/>+ Drone IEIA<br/>+ Voix</b>", ParagraphStyle("lbody",
        fontName="Helvetica-Bold", fontSize=9, textColor=DARK_NAVY,
        alignment=TA_CENTER, leading=13)),
     Paragraph("Multiplayer Netcode<br/>+ Analytics immersif<br/>+ Formation adaptative", styles["table_cell_c"])],
    [Paragraph("2\u20133 semaines", styles["table_cell_c"]),
     Paragraph("<b>5\u20137 semaines</b>", ParagraphStyle("lt",
        fontName="Helvetica-Bold", fontSize=9, textColor=DARK_NAVY, alignment=TA_CENTER)),
     Paragraph("Semestre complet", styles["table_cell_c"])],
]
col_w3 = [W/3, W/3, W/3]
lt = Table(level_data, colWidths=col_w3)
lt.setStyle(TableStyle([
    ("BACKGROUND",    (0,0), (0,-1), colors.HexColor("#64748B")),
    ("BACKGROUND",    (1,0), (1,-1), ORANGE_LITE),
    ("BACKGROUND",    (2,0), (2,-1), DARK_NAVY),
    ("TEXTCOLOR",     (2,0), (2,-1), WHITE),
    ("GRID",          (0,0), (-1,-1), 2, WHITE),
    ("TOPPADDING",    (0,0), (-1,-1), 8),
    ("BOTTOMPADDING", (0,0), (-1,-1), 8),
    ("VALIGN",        (0,0), (-1,-1), "MIDDLE"),
    ("LINEABOVE",     (1,0), (1,-1), 3, ORANGE),
    ("LINEBELOW",     (1,0), (1,-1), 3, ORANGE),
]))
story.append(lt)
story.append(sp(12))

# ── Footer note ───────────────────────────────────────────────────────────────
story.append(hr())
story.append(Paragraph(
    "SmartexVR++ Platform &nbsp;&bull;&nbsp; "
    "Jumeau Num\u00e9rique Industriel &nbsp;&bull;&nbsp; "
    "Secteur Textile Marocain &nbsp;&bull;&nbsp; "
    "CBAM Compliance 2026 &nbsp;&bull;&nbsp; "
    "Document g\u00e9n\u00e9r\u00e9 automatiquement",
    styles["footer_note"]))

# ── Build ─────────────────────────────────────────────────────────────────────
doc.build(story)
print(f"PDF generated: {OUTPUT}")
