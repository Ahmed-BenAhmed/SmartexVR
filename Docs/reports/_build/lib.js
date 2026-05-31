// Shared docx helpers for the SmartexVR reports (A4, French academic style).
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  Header, Footer, AlignmentType, LevelFormat, TabStopType, TabStopPosition,
  TableOfContents, HeadingLevel, BorderStyle, WidthType, ShadingType,
  VerticalAlign, PageNumber, PageBreak, ImageRun,
} = require("docx");
const fs = require("fs");

// A4 content width with 1" (1440 DXA) margins: 11906 - 2880 = 9026
const CONTENT_W = 9026;
const ACCENT = "1F4E79";   // deep blue
const ACCENT2 = "2E75B6";  // lighter blue
const GREY = "595959";

function styles() {
  return {
    default: { document: { run: { font: "Calibri", size: 22 } } }, // 11pt
    paragraphStyles: [
      { id: "Title", name: "Title", basedOn: "Normal", next: "Normal",
        run: { size: 56, bold: true, font: "Calibri", color: ACCENT },
        paragraph: { spacing: { before: 0, after: 120 }, alignment: AlignmentType.CENTER } },
      { id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 30, bold: true, font: "Calibri", color: ACCENT },
        paragraph: { spacing: { before: 280, after: 140 }, outlineLevel: 0,
          border: { bottom: { style: BorderStyle.SINGLE, size: 6, color: ACCENT2, space: 4 } } } },
      { id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 26, bold: true, font: "Calibri", color: ACCENT2 },
        paragraph: { spacing: { before: 200, after: 100 }, outlineLevel: 1 } },
      { id: "Heading3", name: "Heading 3", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 23, bold: true, font: "Calibri", color: "333333" },
        paragraph: { spacing: { before: 140, after: 80 }, outlineLevel: 2 } },
      { id: "Normal", name: "Normal",
        run: { font: "Calibri", size: 22 },
        paragraph: { spacing: { after: 120, line: 276 }, alignment: AlignmentType.JUSTIFIED } },
      { id: "Caption", name: "Caption", basedOn: "Normal", next: "Normal",
        run: { italics: true, size: 18, color: GREY },
        paragraph: { spacing: { before: 40, after: 160 }, alignment: AlignmentType.CENTER } },
    ],
  };
}

function numbering() {
  return {
    config: [
      { reference: "bul", levels: [
        { level: 0, format: LevelFormat.BULLET, text: "•", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 600, hanging: 280 } } } },
        { level: 1, format: LevelFormat.BULLET, text: "–", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 1100, hanging: 280 } } } },
      ]},
      { reference: "num", levels: [
        { level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 600, hanging: 320 } } } },
      ]},
    ],
  };
}

// --- element helpers ---
const H1 = (t) => new Paragraph({ heading: HeadingLevel.HEADING_1, children: [new TextRun(t)] });
const H2 = (t) => new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun(t)] });
const H3 = (t) => new Paragraph({ heading: HeadingLevel.HEADING_3, children: [new TextRun(t)] });

// P accepts a string OR an array of {text, bold, italics, color, font, size}
function P(content, opts = {}) {
  const toRun = (r) => (r instanceof TextRun ? r : new TextRun(r));
  const runs = Array.isArray(content) ? content.map(toRun) : [toRun(content)];
  return new Paragraph({ children: runs, ...opts });
}

function bullet(content, level = 0) {
  const runs = Array.isArray(content) ? content.map((r) => new TextRun(r)) : [new TextRun(content)];
  return new Paragraph({ numbering: { reference: "bul", level }, children: runs });
}
function numItem(content) {
  const runs = Array.isArray(content) ? content.map((r) => new TextRun(r)) : [new TextRun(content)];
  return new Paragraph({ numbering: { reference: "num", level: 0 }, children: runs });
}

const run = (text, o = {}) => new TextRun({ text, ...o });

// monospace code block (single paragraph, shaded)
function code(lines) {
  const arr = Array.isArray(lines) ? lines : lines.split("\n");
  return arr.map((ln, i) =>
    new Paragraph({
      shading: { fill: "F2F2F2", type: ShadingType.CLEAR },
      spacing: { after: i === arr.length - 1 ? 120 : 0, before: i === 0 ? 80 : 0, line: 240 },
      alignment: AlignmentType.LEFT,
      children: [new TextRun({ text: ln || " ", font: "Consolas", size: 18, color: "333333" })],
    })
  );
}

function caption(t) {
  return new Paragraph({ style: "Caption", children: [new TextRun(t)] });
}

// Read intrinsic pixel size of a PNG or JPEG from its buffer.
function intrinsicSize(buf) {
  // PNG: signature then IHDR
  if (buf.length > 24 && buf[0] === 0x89 && buf[1] === 0x50) {
    return { w: buf.readUInt32BE(16), h: buf.readUInt32BE(20) };
  }
  // JPEG: scan SOF markers
  if (buf[0] === 0xff && buf[1] === 0xd8) {
    let o = 2;
    while (o < buf.length) {
      if (buf[o] !== 0xff) { o++; continue; }
      const m = buf[o + 1];
      if (m >= 0xc0 && m <= 0xcf && m !== 0xc4 && m !== 0xc8 && m !== 0xcc) {
        return { h: buf.readUInt16BE(o + 5), w: buf.readUInt16BE(o + 7) };
      }
      o += 2 + buf.readUInt16BE(o + 2);
    }
  }
  return { w: 1.5, h: 1 };
}

// Embed a figure, centered, with a thin frame. Auto-fits within a box
// (maxW x maxH, px @96dpi) preserving aspect ratio. Type inferred from extension.
function image(path, { maxW = 460, maxH = 430, alt = "Figure" } = {}) {
  const ext = path.split(".").pop().toLowerCase();
  const type = ext === "jpeg" ? "jpg" : ext;
  const data = fs.readFileSync(path);
  const { w, h } = intrinsicSize(data);
  const scale = Math.min(maxW / w, maxH / h);
  const width = Math.round(w * scale);
  const height = Math.round(h * scale);
  const frame = { style: BorderStyle.SINGLE, size: 2, color: "D9D9D9", space: 4 };
  return new Paragraph({
    alignment: AlignmentType.CENTER,
    spacing: { before: 100, after: 20 },
    border: { top: frame, bottom: frame, left: frame, right: frame },
    children: [new ImageRun({
      type,
      data,
      transformation: { width, height },
      altText: { title: alt, description: alt, name: alt },
    })],
  });
}

// table: headers = [str], rows = [[str|{text}]], widths sum to CONTENT_W
function table(headers, rows, widths) {
  const b = { style: BorderStyle.SINGLE, size: 1, color: "BFBFBF" };
  const borders = { top: b, bottom: b, left: b, right: b };
  const mk = (text, { head = false, w } = {}) =>
    new TableCell({
      borders,
      width: { size: w, type: WidthType.DXA },
      verticalAlign: VerticalAlign.CENTER,
      shading: head ? { fill: ACCENT, type: ShadingType.CLEAR } : undefined,
      margins: { top: 60, bottom: 60, left: 110, right: 110 },
      children: [new Paragraph({
        alignment: AlignmentType.LEFT,
        spacing: { after: 0, line: 252 },
        children: [new TextRun({ text: String(text), bold: head, color: head ? "FFFFFF" : "000000", size: 20 })],
      })],
    });
  const headRow = new TableRow({
    tableHeader: true,
    children: headers.map((h, i) => mk(h, { head: true, w: widths[i] })),
  });
  const bodyRows = rows.map((r) =>
    new TableRow({
      children: r.map((c, i) => mk(typeof c === "object" ? c.text : c, { w: widths[i] })),
    })
  );
  return new Table({ width: { size: CONTENT_W, type: WidthType.DXA }, columnWidths: widths, rows: [headRow, ...bodyRows] });
}

function spacer(after = 120) {
  return new Paragraph({ spacing: { after }, children: [new TextRun("")] });
}

// Cover page paragraphs
function cover({ school, filiere, year, projectTitle, projectSub, reportType, author, role, supervisor, extra, logos, group, members }) {
  const c = [];
  if (logos && logos.length) {
    const NB = { style: BorderStyle.NONE, size: 0, color: "FFFFFF" };
    const cellNB = { top: NB, bottom: NB, left: NB, right: NB };
    const colW = Math.floor(CONTENT_W / logos.length);
    const cells = logos.map((lg, i) => {
      const data = fs.readFileSync(lg.path);
      const isPng = data[0] === 0x89;
      const ext = lg.path.split(".").pop().toLowerCase();
      const { w, h } = intrinsicSize(data);
      const th = lg.h || 50;
      const tw = Math.round(w * (th / h));
      const align = i === 0 ? AlignmentType.LEFT : (i === logos.length - 1 ? AlignmentType.RIGHT : AlignmentType.CENTER);
      return new TableCell({
        borders: cellNB, width: { size: colW, type: WidthType.DXA }, verticalAlign: VerticalAlign.CENTER,
        children: [new Paragraph({ alignment: align, spacing: { after: 0 },
          children: [new ImageRun({ type: isPng ? "png" : (ext === "jpeg" ? "jpg" : ext), data, transformation: { width: tw, height: th }, altText: { title: "logo", description: "logo", name: "logo" } })] })],
      });
    });
    c.push(new Table({
      width: { size: CONTENT_W, type: WidthType.DXA },
      columnWidths: logos.map(() => colW),
      borders: { top: NB, bottom: NB, left: NB, right: NB, insideHorizontal: NB, insideVertical: NB },
      rows: [new TableRow({ children: cells })],
    }));
    c.push(new Paragraph({ spacing: { after: 240 }, children: [new TextRun("")] }));
  }
  c.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 60 },
    children: [new TextRun({ text: school, bold: true, size: 26, color: ACCENT })] }));
  if (filiere) c.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 40 },
    children: [new TextRun({ text: filiere, size: 22, color: GREY })] }));
  c.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 600 },
    children: [new TextRun({ text: `Année universitaire ${year}`, size: 20, color: GREY })] }));
  c.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 400, after: 0 },
    border: { top: { style: BorderStyle.SINGLE, size: 12, color: ACCENT2, space: 8 } },
    children: [new TextRun({ text: "", size: 8 })] }));
  c.push(new Paragraph({ style: "Title", spacing: { before: 240, after: 60 }, children: [new TextRun(projectTitle)] }));
  if (projectSub) {
    const subLines = projectSub.split("\n");
    c.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 60 },
      children: subLines.map((ln, i) => new TextRun({ text: ln, size: 26, color: "333333", break: i > 0 ? 1 : 0 })) }));
  }
  c.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 60, after: 0 },
    border: { bottom: { style: BorderStyle.SINGLE, size: 12, color: ACCENT2, space: 8 } },
    children: [new TextRun({ text: "", size: 8 })] }));
  c.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 340, after: group ? 160 : 360 },
    children: [new TextRun({ text: reportType, bold: true, size: 30, color: ACCENT })] }));

  if (group) c.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 200 },
    children: [new TextRun({ text: group, bold: true, size: 28, color: ACCENT2 })] }));

  const line = (label, val) => new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 40 },
    children: [new TextRun({ text: `${label} : `, bold: true, size: 22 }), new TextRun({ text: val, size: 22 })] });

  if (members && members.length) {
    c.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 60 },
      children: [new TextRun({ text: "Réalisé par :", bold: true, size: 22 })] }));
    members.forEach((m) => c.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 30 },
      children: [
        new TextRun({ text: m.name, size: 22, bold: true }),
        ...(m.role ? [new TextRun({ text: "  —  " + m.role, size: 20, color: GREY })] : []),
      ] })));
    c.push(new Paragraph({ spacing: { after: 120 }, children: [new TextRun("")] }));
  } else if (author) {
    c.push(line("Réalisé par", author));
  }
  if (role) c.push(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 40 },
    children: [new TextRun({ text: role, italics: true, size: 21, color: GREY })] }));
  if (supervisor) c.push(line("Encadré par", supervisor));
  if (extra) extra.forEach((e) => c.push(line(e[0], e[1])));
  c.push(new Paragraph({ children: [new PageBreak()] }));
  return c;
}

function buildDoc({ coverChildren, tocTitle, body }) {
  const children = [
    ...coverChildren,
    // Title styled like H1 but NOT a heading, so it is not captured by its own TOC field
    new Paragraph({
      spacing: { before: 280, after: 140 },
      border: { bottom: { style: BorderStyle.SINGLE, size: 6, color: ACCENT2, space: 4 } },
      children: [new TextRun({ text: tocTitle || "Sommaire", bold: true, size: 30, color: ACCENT, font: "Calibri" })],
    }),
    new TableOfContents("Sommaire", { hyperlink: true, headingStyleRange: "1-3" }),
    new Paragraph({ children: [new PageBreak()] }),
    ...body,
  ];
  return new Document({
    styles: styles(),
    numbering: numbering(),
    features: { updateFields: true },
    sections: [{
      properties: { page: { size: { width: 11906, height: 16838 }, margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 } } },
      footers: { default: new Footer({ children: [new Paragraph({
        tabStops: [{ type: TabStopType.RIGHT, position: TabStopPosition.MAX }],
        border: { top: { style: BorderStyle.SINGLE, size: 4, color: "BFBFBF", space: 6 } },
        children: [
          new TextRun({ text: "SmartexVR + AR", size: 16, color: GREY }),
          new TextRun({ text: "\tPage ", size: 16, color: GREY }),
          new TextRun({ children: [PageNumber.CURRENT], size: 16, color: GREY }),
          new TextRun({ text: " / ", size: 16, color: GREY }),
          new TextRun({ children: [PageNumber.TOTAL_PAGES], size: 16, color: GREY }),
        ],
      })] }) },
      children,
    }],
  });
}

async function save(doc, outPath) {
  const buf = await Packer.toBuffer(doc);
  fs.writeFileSync(outPath, buf);
  return outPath;
}

module.exports = {
  CONTENT_W, ACCENT, ACCENT2, GREY,
  H1, H2, H3, P, bullet, numItem, run, code, caption, image, table, spacer, cover, buildDoc, save,
  Paragraph, TextRun, PageBreak, AlignmentType,
};
