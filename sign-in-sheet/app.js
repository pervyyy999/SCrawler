const { jsPDF } = window.jspdf;

const form = document.getElementById("signInForm");
const nameInput = document.getElementById("name");
const canvas = document.getElementById("signaturePad");
const clearButton = document.getElementById("clearSignature");
const downloadButton = document.getElementById("downloadPdf");
const ctx = canvas.getContext("2d");

let isDrawing = false;
let lastPoint = { x: 0, y: 0 };
const entries = [];

const resizeCanvasForHiDpi = () => {
  const ratio = window.devicePixelRatio || 1;
  const { width, height } = canvas.getBoundingClientRect();
  canvas.width = Math.max(1, Math.floor(width * ratio));
  canvas.height = Math.max(1, Math.floor(height * ratio));
  ctx.scale(ratio, ratio);
  ctx.lineWidth = 2.5;
  ctx.lineCap = "round";
  ctx.strokeStyle = "#1f2937";
  ctx.fillStyle = "#f8fafc";
  ctx.fillRect(0, 0, width, height);
};

const clearSignature = () => {
  ctx.clearRect(0, 0, canvas.width, canvas.height);
  const { width, height } = canvas.getBoundingClientRect();
  ctx.fillStyle = "#f8fafc";
  ctx.fillRect(0, 0, width, height);
};

const startDrawing = (event) => {
  isDrawing = true;
  lastPoint = getPoint(event);
};

const stopDrawing = () => {
  isDrawing = false;
};

const draw = (event) => {
  if (!isDrawing) return;
  const point = getPoint(event);
  ctx.beginPath();
  ctx.moveTo(lastPoint.x, lastPoint.y);
  ctx.lineTo(point.x, point.y);
  ctx.stroke();
  lastPoint = point;
};

const getPoint = (event) => {
  const rect = canvas.getBoundingClientRect();
  const clientX = event.touches ? event.touches[0].clientX : event.clientX;
  const clientY = event.touches ? event.touches[0].clientY : event.clientY;
  return {
    x: clientX - rect.left,
    y: clientY - rect.top,
  };
};

const hasSignature = () => {
  const { width, height } = canvas;
  const pixels = ctx.getImageData(0, 0, width, height).data;
  for (let i = 0; i < pixels.length; i += 4) {
    if (pixels[i] !== 248 || pixels[i + 1] !== 250 || pixels[i + 2] !== 252) {
      return true;
    }
  }
  return false;
};

const updatePdf = () => {
  const doc = new jsPDF({ unit: "pt", format: "letter" });
  const margin = 40;
  let y = margin;

  doc.setFontSize(18);
  doc.text("Group Counseling Sign-In Sheet", margin, y);
  y += 30;

  entries.forEach((entry, index) => {
    doc.setFontSize(12);
    doc.text(`${index + 1}. ${entry.name}`, margin, y);
    y += 12;

    const imgWidth = 220;
    const imgHeight = 80;
    doc.addImage(entry.signature, "PNG", margin, y, imgWidth, imgHeight);
    y += imgHeight + 18;

    if (y + imgHeight > doc.internal.pageSize.height - margin) {
      doc.addPage();
      y = margin;
    }
  });

  const pdfBlob = doc.output("blob");
  const pdfUrl = URL.createObjectURL(pdfBlob);
  downloadButton.dataset.url = pdfUrl;
  downloadButton.disabled = entries.length === 0;
};

const submitForm = (event) => {
  event.preventDefault();
  const name = nameInput.value.trim();

  if (!name) {
    nameInput.focus();
    return;
  }

  if (!hasSignature()) {
    alert("Please provide a signature before submitting.");
    return;
  }

  const signature = canvas.toDataURL("image/png");
  entries.push({ name, signature });

  updatePdf();
  form.reset();
  clearSignature();
  nameInput.focus();
};

const downloadPdf = () => {
  const url = downloadButton.dataset.url;
  if (!url) return;
  const link = document.createElement("a");
  link.href = url;
  link.download = "group-counseling-sign-in.pdf";
  link.click();
};

window.addEventListener("resize", () => {
  resizeCanvasForHiDpi();
  clearSignature();
});

canvas.addEventListener("pointerdown", startDrawing);
canvas.addEventListener("pointerup", stopDrawing);
canvas.addEventListener("pointerleave", stopDrawing);
canvas.addEventListener("pointermove", draw);
canvas.addEventListener("touchstart", (event) => event.preventDefault());
canvas.addEventListener("touchmove", (event) => event.preventDefault());
clearButton.addEventListener("click", clearSignature);
form.addEventListener("submit", submitForm);
downloadButton.addEventListener("click", downloadPdf);

resizeCanvasForHiDpi();
clearSignature();
