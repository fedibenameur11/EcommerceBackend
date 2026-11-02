using EcommerceAPI.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EcommerceAPI.Services
{
    /// <summary>Génère la facture PDF d'une commande (client / admin).</summary>
    public class OrderDocumentService
    {
        private const string Brand = "TounsiBuy";
        private const string Currency = "TND";
        private static readonly string Primary = Colors.Blue.Darken2;

        static OrderDocumentService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        private static string Money(decimal v) => $"{v:0.000} {Currency}";

        public byte[] BuildInvoice(Order order)
        {
            var subtotal = order.Items.Sum(i => i.TotalPrice);
            var total = subtotal - order.DiscountAmount + order.DeliveryFee;

            return Document.Create(doc => doc.Page(page =>
            {
                Setup(page);
                page.Header().Element(c => Header(c, "FACTURE", order));
                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(14);
                    col.Item().Element(c => CustomerBlock(c, order));
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5); c.RelativeColumn(1); c.RelativeColumn(2); c.RelativeColumn(2);
                        });
                        t.Header(h =>
                        {
                            HeaderCell(h.Cell(), "Désignation");
                            HeaderCell(h.Cell(), "Qté", true);
                            HeaderCell(h.Cell(), "Prix unitaire", true);
                            HeaderCell(h.Cell(), "Total", true);
                        });
                        foreach (var i in order.Items)
                        {
                            BodyCell(t.Cell(), Designation(i));
                            BodyCell(t.Cell(), i.Quantity.ToString(), true);
                            BodyCell(t.Cell(), Money(i.UnitPrice), true);
                            BodyCell(t.Cell(), Money(i.TotalPrice), true);
                        }
                    });
                    col.Item().AlignRight().Width(240).Column(s =>
                    {
                        TotalLine(s, "Sous-total", Money(subtotal));
                        if (order.DiscountAmount > 0)
                            TotalLine(s, $"Remise ({order.CouponCode})", "-" + Money(order.DiscountAmount));
                        TotalLine(s, "Livraison", Money(order.DeliveryFee));
                        s.Item().PaddingTop(4).BorderTop(1).BorderColor(Primary).PaddingTop(4)
                            .Row(r =>
                            {
                                r.RelativeItem().Text("Total à payer").Bold().FontSize(12);
                                r.AutoItem().Text(Money(total)).Bold().FontSize(12).FontColor(Primary);
                            });
                    });
                    if (order.DeliveryPerson != null)
                        col.Item().Text($"Livreur : {PersonName(order.DeliveryPerson)}").FontColor(Colors.Grey.Darken1);
                    col.Item().Text("Paiement : espèces à la livraison.").FontColor(Colors.Grey.Darken1);
                });
                page.Footer().AlignCenter().Text($"{Brand} - Merci pour votre confiance").FontSize(9).FontColor(Colors.Grey.Medium);
            })).GeneratePdf();
        }

        private static void Setup(PageDescriptor page)
        {
            page.Size(PageSizes.A4);
            page.Margin(35);
            page.DefaultTextStyle(x => x.FontSize(10));
        }

        private static void Header(IContainer c, string title, Order order)
        {
            c.Row(r =>
            {
                r.RelativeItem().Column(col =>
                {
                    col.Item().Text(Brand).Bold().FontSize(22).FontColor(Primary);
                    col.Item().Text("tunisie - boutique en ligne").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
                r.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text(title).Bold().FontSize(16);
                    col.Item().Text($"Commande {order.OrderNumber}");
                    col.Item().Text($"Date : {order.OrderDate.ToLocalTime():dd/MM/yyyy}");
                });
            });
        }

        private static void CustomerBlock(IContainer c, Order order)
        {
            c.Background(Colors.Grey.Lighten4).Padding(10).Column(col =>
            {
                col.Item().Text("Client").SemiBold();
                col.Item().Text($"{order.User?.FirstName} {order.User?.LastName}".Trim());
                if (!string.IsNullOrWhiteSpace(order.User?.PhoneNumber) && order.User!.PhoneNumber != "-")
                    col.Item().Text($"Tél. : {order.User.PhoneNumber}");
                col.Item().Text($"Adresse de livraison : {order.ShippingAddress}");
            });
        }

        private static string PersonName(User u) => $"{u.FirstName} {u.LastName}".Trim();

        private static string Designation(OrderItem i)
        {
            var name = i.Product?.Name ?? "Produit indisponible";
            return string.IsNullOrWhiteSpace(i.Size) ? name : $"{name} (taille {i.Size})";
        }

        private static void HeaderCell(IContainer c, string text, bool right = false)
        {
            var cell = c.Background(Primary).Padding(6);
            (right ? cell.AlignRight() : cell).Text(text).Bold().FontColor(Colors.White);
        }

        private static void BodyCell(IContainer c, string text, bool right = false)
        {
            var cell = c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6);
            (right ? cell.AlignRight() : cell).Text(text);
        }

        private static void TotalLine(ColumnDescriptor col, string label, string value)
            => col.Item().PaddingVertical(2).Row(r =>
            {
                r.RelativeItem().Text(label);
                r.AutoItem().Text(value);
            });
    }
}
