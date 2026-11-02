namespace EcommerceAPI.Models
{
    /// <summary>Valeurs autorisées pour le public visé et les tailles d'un produit.</summary>
    public static class ProductOptions
    {
        public const string Men = "Homme";
        public const string Women = "Femme";
        public static readonly string[] Genders = { Men, Women };

        /// <summary>Couleurs principales d'un produit (la teinte affichée côté web est définie dans le frontend).</summary>
        public static readonly string[] Colors =
            { "Noir", "Blanc", "Gris", "Beige", "Marron", "Rouge", "Orange", "Jaune", "Vert", "Bleu", "Violet", "Rose" };

        /// <summary>Pointures des chaussures (espadrilles...).</summary>
        public static readonly string[] ShoeSizes = { "38", "39", "40", "41", "42", "43", "44", "45" };
        /// <summary>Tailles des vêtements.</summary>
        public static readonly string[] ClothingSizes = { "XS", "S", "M", "L", "XL", "XXL" };

        public static readonly string[] AllSizes = ShoeSizes.Concat(ClothingSizes).ToArray();
    }
}
