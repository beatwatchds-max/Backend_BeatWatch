using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Models;
using MongoDB.Driver;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
// NUEVOS USINGS PARA LAS FUENTES
using iText.Kernel.Font;
using iText.IO.Font.Constants;

namespace BeatWatch_BackEnd.Services
{
    public class ReporteService : IReporteService
    {
        private readonly MongoDbContext _context;

        public ReporteService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> GenerarPdfReciboAsync(string licenciaId)
        {
            // 1. Buscar la licencia en la base de datos
            var licencia = await _context.Licencias
                .Find(l => l.Id == licenciaId)
                .FirstOrDefaultAsync();

            if (licencia == null)
            {
                throw new KeyNotFoundException("La licencia especificada no existe.");
            }

            // 2. Buscar datos complementarios del usuario
            var usuario = await _context.Usuarios
                .Find(u => u.Id == licencia.UsuarioId)
                .FirstOrDefaultAsync();

            using (var memoryStream = new MemoryStream())
            {
                // Inicializar iText7
                var writer = new PdfWriter(memoryStream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                // --- CREACIÓN DE FUENTES (CORRECCIÓN DEFINITIVA) ---
                PdfFont fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                PdfFont fontItalic = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

                // --- ESTILOS Y MAQUETACIÓN DEL COMPROBANTE ---

                // Encabezado Principal
                Text textoTitulo = new Text("COMPROBANTE DE PAGO - BEATWATCH")
                    .SetFont(fontBold) // Sustituye a SetBold()
                    .SetFontSize(20)
                    .SetFontColor(ColorConstants.BLUE);

                Paragraph titulo = new Paragraph().Add(textoTitulo).SetTextAlignment(TextAlignment.CENTER);
                document.Add(titulo);

                document.Add(new Paragraph($"Fecha de Emisión: {DateTime.Now:dd/MM/yyyy HH:mm}"));
                document.Add(new Paragraph(new string('-', 80)));

                // Sección: Datos del Titular
                Text textoSeccion1 = new Text("DATOS DEL RESPONSABLE").SetFont(fontBold).SetFontSize(14);
                document.Add(new Paragraph().Add(textoSeccion1));

                document.Add(new Paragraph($"Nombre Completo: {(usuario != null ? usuario.Nombre : "Cliente BeatWatch Ext")}"));
                document.Add(new Paragraph($"Correo Electrónico: {(usuario != null ? usuario.Correo : "N/D")}"));

                if (usuario != null && !string.IsNullOrEmpty(usuario.EmpresaOrganizacion))
                {
                    document.Add(new Paragraph($"Empresa: {usuario.EmpresaOrganizacion}"));
                    document.Add(new Paragraph($"RFC: {usuario.RFC}"));
                }

                document.Add(new Paragraph(new string('-', 80)));

                // Sección: Detalle de la Licencia Adquirida
                Text textoSeccion2 = new Text("DETALLE DE LA LICENCIA").SetFont(fontBold).SetFontSize(14);
                document.Add(new Paragraph().Add(textoSeccion2));

                // Tabla de desglose comercial
                float[] anchosColumnas = { 1f, 2f };
                var tabla = new Table(UnitValue.CreatePercentArray(anchosColumnas));
                tabla.SetWidth(UnitValue.CreatePercentValue(100));

                tabla.AddCell(new Cell().Add(new Paragraph("Concepto").SetFont(fontBold)));
                tabla.AddCell(new Cell().Add(new Paragraph("Detalle").SetFont(fontBold)));

                tabla.AddCell(new Cell().Add(new Paragraph("ID Transacción / Licencia")));
                tabla.AddCell(new Cell().Add(new Paragraph(licencia.Id ?? "N/D")));

                tabla.AddCell(new Cell().Add(new Paragraph("Tipo de Plan")));
                tabla.AddCell(new Cell().Add(new Paragraph(licencia.Tipo)));

                tabla.AddCell(new Cell().Add(new Paragraph("Código de Grupo Generado")));
                tabla.AddCell(new Cell().Add(new Paragraph(licencia.CodigoGrupo)));

                tabla.AddCell(new Cell().Add(new Paragraph("Método de Pago Seleccionado")));
                tabla.AddCell(new Cell().Add(new Paragraph(licencia.MetodoPago)));

                tabla.AddCell(new Cell().Add(new Paragraph("Estado del Cobro")));

                // Estilo dinámico para el estado del pago
                string estado = licencia.EstadoPago ?? "Pendiente";
                Text textoEstado = new Text(estado)
                    .SetFontColor(estado.ToUpper() == "APROBADO" ? ColorConstants.GREEN : ColorConstants.RED);
                tabla.AddCell(new Cell().Add(new Paragraph().Add(textoEstado)));

                tabla.AddCell(new Cell().Add(new Paragraph("Vigencia")));
                tabla.AddCell(new Cell().Add(new Paragraph($"Del {licencia.FechaInicio:dd/MM/yyyy} al {licencia.FechaFin:dd/MM/yyyy}")));

                document.Add(tabla);

                document.Add(new Paragraph("\n\n"));
                var piePagina = new Paragraph("¡Gracias por confiar en BeatWatch para el monitoreo de salud cardíaca!")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFont(fontItalic) // Sustituye a SetItalic()
                    .SetFontSize(10);
                document.Add(piePagina);

                // Cerrar el documento para escribir el buffer en el stream
                document.Close();

                return memoryStream.ToArray();
            }
        }
    }
}