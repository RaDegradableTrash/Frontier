import fs from "node:fs/promises";
import path from "node:path";
import { Presentation, PresentationFile } from "@oai/artifact-tool";

const root = "/Users/ra/Documents/Frontier/outputs/kards-deck";
const assetDir = path.join(root, "assets");
const previewDir = path.join(root, "preview");
const layoutDir = path.join(root, "layout");
const finalPptx = path.join(root, "kards-design-workflow-deck.pptx");

async function writeBlob(filePath, blob) {
  await fs.writeFile(filePath, new Uint8Array(await blob.arrayBuffer()));
}

async function imageBytes(fileName) {
  return fs.readFile(path.join(assetDir, fileName));
}

const images = [
  "ig_045b44d7810878af016a3bf5c1fe688198aa7963ef9ade24bd.png",
  "ig_045b44d7810878af016a3bf5fd7c5081989a2b88afde6269ef.png",
  "ig_045b44d7810878af016a3bf63861648198846ba4aebbf20013.png",
  "ig_045b44d7810878af016a3bf6763f4c81988c8ac0ad7fa8da5f.png",
  "ig_045b44d7810878af016a3bf6b02930819894972decec2dd196.png",
  "ig_045b44d7810878af016a3bf6ea3db08198850e93766e443011.png",
  "ig_045b44d7810878af016a3bf727e36881988f001a9c5c155d6b.png",
  "ig_045b44d7810878af016a3bf769ff8881989d79b41832d53f22.png",
  "ig_045b44d7810878af016a3bf7aa9fb48198876e78ab9a4c3032.png",
  "ig_045b44d7810878af016a3bf7e8c96c8198be1e7cb3c7edb94d.png",
];

const slides = [
  {
    kicker: "FRONTIER / KARDS-STYLE PROTOTYPE",
    title: "Kards Design & Workflow",
    subtitle: "A Unity tabletop card-battler foundation built around clear zones, escalating Kredits, tactile board actions, and fast player feedback.",
    bullets: ["Unity 2022.3 prototype", "Original assets and implementation", "KARDS-like UX patterns, not a clone"],
    align: "left",
  },
  {
    kicker: "DESIGN INTENT",
    title: "Make Every Tactical Choice Visible",
    subtitle: "The prototype is organized around a small set of board truths that players can read at a glance.",
    bullets: ["Readable card state and legal targets", "Physical-feeling tabletop movement", "Simple feedback before deeper art polish"],
    align: "right",
  },
  {
    kicker: "BOARD MODEL",
    title: "Three Rows, Two Headquarters",
    subtitle: "Runtime board generation creates enemy support, frontline, and player support rows around opposing HQs.",
    bullets: ["Support rows stage deployed units", "Frontline controls pressure and attacks", "HQ health defines win and loss states"],
    align: "left",
  },
  {
    kicker: "RESOURCES & ZONES",
    title: "Cards Move Through a Small State Machine",
    subtitle: "Deck, hand, board, countermeasures, and discard are backed by runtime card state and escalating Kredits.",
    bullets: ["CardData becomes RuntimeCard", "Kredits gate deploy and operation costs", "Deck, hand, discard, board, and set traps"],
    align: "left",
  },
  {
    kicker: "MATCH FLOW",
    title: "From Starter Deck to End Turn Loop",
    subtitle: "The current flow starts with deck choice, runs a mulligan, then cycles draw, refresh, action, enemy AI, and game-over checks.",
    bullets: ["Choose starter deck", "Keep or mulligan opening hand", "Act, end turn, resolve enemy response"],
    align: "right",
  },
  {
    kicker: "PLAYER WORKFLOW",
    title: "Select or Drag, Then Commit",
    subtitle: "The interaction model supports click and drag flows for units, orders, and countermeasures.",
    bullets: ["Units deploy to highlighted support slots", "Orders resolve on legal targets or board slots", "Countermeasures set face-down for later triggers"],
    align: "left",
  },
  {
    kicker: "FRONTLINE WORKFLOW",
    title: "Advance Units to Create Pressure",
    subtitle: "Support units spend operation cost to move forward, while frontline control limits when new units can advance.",
    bullets: ["Move from support to empty frontline slots", "Control rules prevent free reinforcement", "Pinned and other states shape timing"],
    align: "right",
  },
  {
    kicker: "COMBAT RESOLUTION",
    title: "Attack Units, Break HQ, Trigger Traps",
    subtitle: "Frontline units attack enemy support units or the enemy HQ, with guard restrictions and countermeasure reactions.",
    bullets: ["Damage, repair, buffs, draw, and pin effects", "Keywords include Blitz, Guard, Fury, Smokescreen, Pinned", "Victory, defeat, and draw states are explicit"],
    align: "right",
    subtitleTop: 336,
  },
  {
    kicker: "IMPLEMENTATION SHAPE",
    title: "Lean Runtime Layers Keep the Prototype Movable",
    subtitle: "The code separates card definitions, runtime state, board slots, card visuals, and the main turn controller.",
    bullets: ["CardData and starter deck archetypes define content", "GameController coordinates phases, actions, AI, and status", "BoardManager, SlotInteract, and CardView drive tabletop feedback"],
    align: "left",
  },
  {
    kicker: "NEXT TARGETS",
    title: "Polish the Surface, Deepen the System",
    subtitle: "The roadmap moves from placeholder visuals and broad mechanics toward a richer card-game product loop.",
    bullets: ["World-space card prefabs and animated drag previews", "Expanded timing windows, keywords, nations, and targeting", "Deck builder, collection data, VFX, sound, and state panels"],
    align: "left",
  },
];

function addTextBox(slide, text, position, style) {
  const shape = slide.shapes.add({
    geometry: "textbox",
    position,
    fill: "none",
    line: { style: "solid", fill: "none", width: 0 },
  });
  shape.text = text;
  shape.text.style = style;
  return shape;
}

function addOverlay(slide, align) {
  slide.shapes.add({
    geometry: "rect",
    position: { left: 0, top: 0, width: 1280, height: 720 },
    fill: "linear(90deg, #020713/88 0%, #07101f/62 38%, #07101f/24 68%, #020713/66 100%)",
    line: { style: "solid", fill: "none", width: 0 },
  });

  const panelLeft = align === "right" ? 696 : 56;
  slide.shapes.add({
    geometry: "roundRect",
    position: { left: panelLeft, top: 76, width: 528, height: 568 },
    fill: "#06101d/76",
    line: { style: "solid", fill: "#d4af37/32", width: 1 },
    borderRadius: 8,
    shadow: "shadow-lg",
  });

  slide.shapes.add({
    geometry: "rect",
    position: { left: panelLeft, top: 76, width: 6, height: 568 },
    fill: "#d4af37",
    line: { style: "solid", fill: "none", width: 0 },
  });

  return panelLeft;
}

async function addSlide(presentation, spec, index) {
  const slide = presentation.slides.add();
  slide.background.fill = "#020713";
  slide.images.add({
    blob: await imageBytes(images[index]),
    contentType: "image/png",
    alt: `${spec.title} slide artwork`,
    fit: "cover",
    position: { left: 0, top: 0, width: 1280, height: 720 },
  });

  const x = addOverlay(slide, spec.align);
  addTextBox(slide, spec.kicker, { left: x + 44, top: 118, width: 440, height: 30 }, {
    fontSize: 15,
    bold: true,
    color: "#d4af37",
  });
  addTextBox(slide, spec.title, { left: x + 44, top: 166, width: 430, height: 150 }, {
    fontSize: index === 0 ? 54 : 42,
    bold: true,
    color: "#f8fafc",
  });
  addTextBox(slide, spec.subtitle, { left: x + 44, top: spec.subtitleTop ?? (index === 0 ? 326 : 306), width: 432, height: 96 }, {
    fontSize: 21,
    color: "#dbe5f0",
  });

  spec.bullets.forEach((bullet, bulletIndex) => {
    const y = 438 + bulletIndex * 58;
    slide.shapes.add({
      geometry: "ellipse",
      position: { left: x + 46, top: y + 9, width: 12, height: 12 },
      fill: bulletIndex === 0 ? "#52d1ff" : "#d4af37",
      line: { style: "solid", fill: "none", width: 0 },
    });
    addTextBox(slide, bullet, { left: x + 72, top: y, width: 390, height: 44 }, {
      fontSize: 18,
      color: "#f1f5f9",
    });
  });

  addTextBox(slide, `0${index + 1}`.slice(-2), { left: 1130, top: 646, width: 64, height: 28 }, {
    fontSize: 16,
    bold: true,
    color: "#d4af37",
  });
}

async function main() {
  await fs.mkdir(previewDir, { recursive: true });
  await fs.mkdir(layoutDir, { recursive: true });

  const presentation = Presentation.create({
    slideSize: { width: 1280, height: 720 },
  });

  for (let i = 0; i < slides.length; i += 1) {
    await addSlide(presentation, slides[i], i);
  }

  for (const [index, slide] of presentation.slides.items.entries()) {
    const stem = `slide-${String(index + 1).padStart(2, "0")}`;
    await writeBlob(path.join(previewDir, `${stem}.png`), await presentation.export({ slide, format: "png", scale: 1 }));
    const layout = await slide.export({ format: "layout" });
    await fs.writeFile(path.join(layoutDir, `${stem}.layout.json`), await layout.text());
  }

  await writeBlob(path.join(root, "kards-design-workflow-montage.webp"), await presentation.export({
    format: "webp",
    montage: true,
    scale: 1,
  }));

  const pptx = await PresentationFile.exportPptx(presentation);
  await pptx.save(finalPptx);
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
