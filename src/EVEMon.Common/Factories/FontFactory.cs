using System;
using System.Drawing;
using EVEMon.Common.Extensions;
using EVEMon.Common.Helpers;

namespace EVEMon.Common.Factories
{
    /// <summary>
    /// A simple font helper factory.
    /// </summary>
    public static class FontFactory
    {
        /// <summary>
        /// Variable to store default font in.
        /// </summary>
        /// <remarks>
        /// DefaultFont is cached for the following reasons:
        /// 1) improve the display time of the traybar popup.
        /// 2) remove the 50+ TrueType exceptions which SystemFonts.DefaultFont generates (at least on WinXP).
        /// </remarks>
        private static Font s_cachedDefaultFont;

        /// <summary>
        /// Gets the default font.
        /// </summary>
        /// <value>The default font.</value>
        private static Font DefaultFont => s_cachedDefaultFont ?? (s_cachedDefaultFont = SystemFonts.DefaultFont);


        #region Helpers for default font

        /// <summary>
        /// Gets the default font.
        /// </summary>
        /// <returns></returns>
        public static Font GetDefaultFont() => DefaultFont;

        /// <summary>
        /// Gets the default font.
        /// </summary>
        /// <param name="style">The style.</param>
        /// <returns></returns>
        public static Font GetDefaultFont(FontStyle style) => GetDefaultFont(SystemFonts.DefaultFont.Size, style);

        /// <summary>
        /// Gets the default font.
        /// </summary>
        /// <param name="emSize">Size of the em, in the given unit.</param>
        /// <param name="style">The style.</param>
        /// <param name="unit">Units for the size : pixels, points, etc. Default should be point.</param>
        /// <returns></returns>
        public static Font GetDefaultFont(float emSize, FontStyle style = FontStyle.Regular,
            GraphicsUnit unit = GraphicsUnit.Point) => GetFont(DefaultFont.FontFamily.Name, emSize, style, unit);

        #endregion


        #region Helpers for non-default font

        /// <summary>
        /// Gets the specified font.
        /// </summary>
        /// <param name="fontName">The font's name</param>
        /// <param name="style">The font's style</param>
        /// <returns></returns>
        public static Font GetFont(string fontName, FontStyle style = FontStyle.Regular)
            => GetFont(fontName, DefaultFont.Size, style, DefaultFont.Unit);

        /// <summary>
        /// Gets the specified font.
        /// </summary>
        /// <param name="prototype">The font's prototype this font will be based on</param>
        /// <param name="style">The overriden style.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">prototype</exception>
        public static Font GetFont(Font prototype, FontStyle style = FontStyle.Regular)
        {
            prototype.ThrowIfNull(nameof(prototype));

            return GetFont(prototype.FontFamily.Name, prototype.Size, style, prototype.Unit);
        }

        /// <summary>
        /// Gets the specified font.
        /// </summary>
        /// <param name="family">The font's family</param>
        /// <param name="emSize">Size of the font, in the provided unit.</param>
        /// <param name="style">The font's style.</param>
        /// <param name="unit">The unit to use for the given size (points, pixels, etc)</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">family</exception>
        public static Font GetFont(FontFamily family, float emSize, FontStyle style = FontStyle.Regular,
            GraphicsUnit unit = GraphicsUnit.Point)
        {
            family.ThrowIfNull(nameof(family));

            return GetFont(family.Name, emSize, style, unit);
        }

        #endregion


        /// <summary>
        /// Gets the specified font.
        /// </summary>
        /// <param name="familyName">The font's family</param>
        /// <param name="emSize">Size of the font, in the given unit.</param>
        /// <param name="style">The font's style.</param>
        /// <param name="unit">Units for the size : pixels, points, etc. Default is point.</param>
        /// <returns></returns>
        public static Font GetFont(string familyName, float emSize, FontStyle style = FontStyle.Regular,
            GraphicsUnit unit = GraphicsUnit.Point)
        {
            // Map legacy font names to modern equivalents for consistent rendering
            if (string.Equals(familyName, "Tahoma", StringComparison.OrdinalIgnoreCase))
                familyName = "Segoe UI";

            try
            {
                FontFamily family = null;
                try
                {
                    try
                    {
                        // Initial try
                        family = new FontFamily(familyName); // Will accept anything and won't throw an error
                        return new Font(familyName, emSize, style, unit);
                    }
                    catch (ArgumentException e)
                    {
                        // First fallback : default family
                        ExceptionHandler.LogException(e, true);
                        return new Font(DefaultFont.FontFamily, emSize, style, unit);
                    }
                }
                catch (ArgumentException e)
                {
                    // Second fallback : default family and style
                    ExceptionHandler.LogException(e, true);
                    family = family ?? DefaultFont.FontFamily;
                    return new Font(family, emSize, DefaultFont.Style, unit);
                }
                finally
                {
                    family?.Dispose();
                }
            }
            catch (ArgumentException e)
            {
                // Third fallback : all to default
                ExceptionHandler.LogException(e, true);
                return DefaultFont;
            }
        }
    }
}