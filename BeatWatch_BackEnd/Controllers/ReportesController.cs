using Microsoft.AspNetCore.Mvc;
using BeatWatch_BackEnd.Services;

namespace BeatWatch_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly IReporteService _reporteService;

        public ReportesController(IReporteService reporteService)
        {
            _reporteService = reporteService;
        }

        [HttpGet("descargar/recibo/{id}")]
        public async Task<IActionResult> DescargarRecibo(string id)
        {
            try
            {
                byte[] pdfBytes = await _reporteService.GenerarPdfReciboAsync(id);

                // Retornar archivo binario con el tipo MIME correspondiente a PDFs
                string nombreArchivo = $"Recibo_BeatWatch_{id}.pdf";
                return File(pdfBytes, "application/pdf", nombreArchivo);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al compilar el comprobante PDF.", detalle = ex.Message });
            }
        }
    }
}