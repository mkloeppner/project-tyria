namespace ProjectTyrant
{
    /// <summary>Lauf 5: Element-Paletten des VFX-Baukastens (Farbe, Textur-Set, Partikelverhalten, Licht, Sound-Familie).</summary>
    public enum VfxPalette { Feuer, Wasser, Luft, Erde, Natur, Schatten, Licht, Blut, Arkan, Frost, Glut, Mondlicht, Wildblut, Dorngift, Himmel, Gewitter, Zwielicht, Jade, Lava, Pulver, Staubgold, Tiefviolett, Stahl, Richtglut, Bollwerk, Sturmblau, Smaragd }

    /// <summary>Formen des VFX-Baukastens.</summary>
    public enum VfxShape { Charge, Release, Projectile, Beam, Cone, GroundCircle, Pillar, Shockwave, Aura, Impact, Trail, Sigil, Veil }

    public enum VfxSize { S, M, L, Elite }

    /// <summary>Kompositions-Schlüssel „Form/Palette/Größe“ und SFX-Schlüssel „Palette/Ereignis“ (rein).</summary>
    public static class VfxIds
    {
        public static string Compose(VfxShape shape, VfxPalette palette, VfxSize size) => shape + "/" + palette + "/" + size;
        public static string Sfx(VfxPalette palette, string evt) => palette + "/" + evt;

        public static bool TryParse(string id, out VfxShape shape, out VfxPalette palette, out VfxSize size)
        {
            shape = VfxShape.Impact; palette = VfxPalette.Licht; size = VfxSize.M;
            if (string.IsNullOrEmpty(id)) return false;
            var p = id.Split('/');
            if (p.Length != 3) return false;
            return System.Enum.TryParse(p[0], out shape) && System.Enum.TryParse(p[1], out palette) && System.Enum.TryParse(p[2], out size);
        }
    }
}
