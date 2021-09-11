using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FBDR.Controls
{
    /// <summary>
    /// Interaction logic for FontPicker.xaml
    /// </summary>
    public partial class FontPicker : UserControl
    {


        public List<FamilyTypeface> AvailableTypefaces
        {
            get { return (List<FamilyTypeface>)GetValue(AvailableTypefacesProperty); }
            set { SetValue(AvailableTypefacesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AvailableTypefaces.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AvailableTypefacesProperty =
            DependencyProperty.Register("AvailableTypefaces", typeof(List<FamilyTypeface>), typeof(FontPicker),
                new PropertyMetadata(new List<FamilyTypeface>()));


        public Models.Font SelectedFont
        {
            get { return (Models.Font)GetValue(SelectedFontProperty); }
            set { SetValue(SelectedFontProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedFont.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedFontProperty =
            DependencyProperty.Register("SelectedFont", typeof(Models.Font), typeof(FontPicker),
                 new FrameworkPropertyMetadata(new Models.Font(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                     OnFontChanged));

        public Models.Color InvertedColor
        {
            get { return (Models.Color)GetValue(InvertedColorProperty); }
            set { SetValue(InvertedColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InvertedColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InvertedColorProperty =
            DependencyProperty.Register("InvertedColor", typeof(Models.Color), typeof(FontPicker),
                new PropertyMetadata(null));

        public double Size
        {
            get { return (double)GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Size.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register("Size", typeof(double), typeof(FontPicker),
                new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSizeChanged));

        public Models.Color SelectedColor
        {
            get { return (Models.Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Models.Color), typeof(FontPicker),
                new FrameworkPropertyMetadata(new Models.Color(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorChanged));

        public FamilyTypeface SelectedTypeface
        {
            get { return (FamilyTypeface)GetValue(SelectedTypefaceProperty); }
            set { SetValue(SelectedTypefaceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Typeface.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedTypefaceProperty =
            DependencyProperty.Register("SelectedTypeface", typeof(FamilyTypeface), typeof(FontPicker),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTypeFaceChanged));

        public FontFamily SelectedFontFamily
        {
            get { return (FontFamily)GetValue(SelectedFontFamilyProperty); }
            set { SetValue(SelectedFontFamilyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedFontFamily.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedFontFamilyProperty =
            DependencyProperty.Register("SelectedFontFamily", typeof(FontFamily), typeof(FontPicker),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFontFamilyChanged));

        public FontPicker()
        {
            InitializeComponent();
        }

        private Models.Style TypefaceToStyleFlags(FamilyTypeface typeface)
        {
            var style = Models.Style.Regular;
            if (typeface.Weight == FontWeights.Bold)
            {
                style |= Models.Style.Bold;
            }
            if (typeface.Style == FontStyles.Italic)
            {
                style |= Models.Style.Italic;
            }
            return style;
        }

        private FamilyTypeface StyleFlagsToTypeface(Models.Style style)
        {
            var fontWeight = style.HasFlag(Models.Style.Bold) ? FontWeights.Bold : FontWeights.Regular;
            var fontStyle = style.HasFlag(Models.Style.Italic) ? FontStyles.Italic : FontStyles.Normal;
            return SelectedFontFamily.FamilyTypefaces.FirstOrDefault(x => x.Weight == fontWeight
            && x.Style == fontStyle);
        }

        private static void OnTypeFaceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as FontPicker)?.OnTypeFaceChanged(e.NewValue as FamilyTypeface);

        private void OnTypeFaceChanged(FamilyTypeface familyTypeface)
        {
            SelectedFont.Style = TypefaceToStyleFlags(familyTypeface);
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as FontPicker)?.OnSizeChanged((double)e.NewValue);

        private void OnSizeChanged(double size)
        {
            SelectedFont.Size = size;
        }

        private static void OnFontFamilyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as FontPicker)?.OnFontFamilyChanged(e.NewValue as FontFamily);

        private void OnFontFamilyChanged(FontFamily fontFamily)
        {
            var allowedStyles = new List<FontStyle>{ FontStyles.Normal, FontStyles.Italic };
            var allowedWeights = new List<FontWeight>{ FontWeights.Regular, FontWeights.Bold };
            AvailableTypefaces = fontFamily.FamilyTypefaces.Where(x => allowedStyles.Contains(x.Style)
            && allowedWeights.Contains(x.Weight)).ToList();

            SelectedFont.FontFamily = fontFamily.ToString();
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as FontPicker)?.OnColorChanged(e.NewValue as Models.Color);

        private void OnColorChanged(Models.Color color)
        {
            InvertedColor = color?.Invert();
            SelectedFont.Color = color;
        }

        private static void OnFontChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as FontPicker)?.OnFontChanged(e.NewValue as Models.Font);

        private void OnFontChanged(Models.Font font)
        {
            if (font is null)
            {
                return;
            }
            SelectedFontFamily = Fonts.SystemFontFamilies.FirstOrDefault(x => x.ToString().ToLower() == font?.FontFamily.ToLower());
            SelectedTypeface = StyleFlagsToTypeface(font.Style);
            SelectedColor = font?.Color;
            Size = font.Size;
        }
    }
}
