// Confirmación genérica para formularios con data-confirmar="mensaje".
document.addEventListener("submit", function (e) {
  var form = e.target;
  var mensaje = form.getAttribute("data-confirmar");
  if (mensaje && !window.confirm(mensaje)) {
    e.preventDefault();
  }
});
