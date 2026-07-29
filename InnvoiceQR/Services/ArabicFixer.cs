using System;
using System.Collections.Generic;
using System.Text;

namespace InnvoiceQR.Services
{
    /// <summary>
    /// معالج النصوص العربية لعرضها بشكل صحيح في ملفات PDF (iText 7).
    ///
    /// المنطق:
    ///   1. نقسّم النص إلى مقاطع: عربي / غير عربي (أرقام + لاتيني).
    ///   2. كل مقطع عربي يمر بـ Contextual Shaping (تحديد شكل كل حرف).
    ///   3. نعكس ترتيب المقاطع + نعكس كل مقطع عربي داخلياً (Visual Reorder).
    ///   4. المقاطع غير العربية لا تُعكس داخلياً، فقط يتبدّل موقعها.
    ///
    /// ⚠ يجب استخدام خط يدعم Arabic Presentation Forms (U+FE70-FEFF)
    ///   مثل Amiri أو Scheherazade أو Traditional Arabic.
    ///   خط Cairo لا يدعم هذا النطاق ويُسبّب مربعات فارغة.
    /// </summary>
    public static class ArabicFixer
    {
        // ─────────────────────────────────────────────
        // هياكل البيانات
        // ─────────────────────────────────────────────

        private readonly struct ArabicForm
        {
            public readonly int Isolated, Initial, Medial, Final;
            public ArabicForm(int isolated, int initial, int medial, int final)
            {
                Isolated = isolated;
                Initial = initial;
                Medial = medial;
                Final = final;
            }
        }

        // ─────────────────────────────────────────────
        // جدول أشكال الحروف (Arabic Presentation Forms-B)
        // ─────────────────────────────────────────────

        private static readonly Dictionary<char, ArabicForm> CharMap =
            new Dictionary<char, ArabicForm>
            {
                { 'ء', new ArabicForm(0xFE80, 0xFE80, 0xFE80, 0xFE80) },
                { 'آ', new ArabicForm(0xFE81, 0xFE81, 0xFE82, 0xFE82) },
                { 'أ', new ArabicForm(0xFE83, 0xFE83, 0xFE84, 0xFE84) },
                { 'ؤ', new ArabicForm(0xFE85, 0xFE85, 0xFE86, 0xFE86) },
                { 'إ', new ArabicForm(0xFE87, 0xFE87, 0xFE88, 0xFE88) },
                { 'ئ', new ArabicForm(0xFE89, 0xFE8B, 0xFE8C, 0xFE8A) },
                { 'ا', new ArabicForm(0xFE8D, 0xFE8D, 0xFE8E, 0xFE8E) },
                { 'ب', new ArabicForm(0xFE8F, 0xFE91, 0xFE92, 0xFE90) },
                { 'ة', new ArabicForm(0xFE93, 0xFE93, 0xFE94, 0xFE94) },
                { 'ت', new ArabicForm(0xFE95, 0xFE97, 0xFE98, 0xFE96) },
                { 'ث', new ArabicForm(0xFE99, 0xFE9B, 0xFE9C, 0xFE9A) },
                { 'ج', new ArabicForm(0xFE9D, 0xFE9F, 0xFEA0, 0xFE9E) },
                { 'ح', new ArabicForm(0xFEA1, 0xFEA3, 0xFEA4, 0xFEA2) },
                { 'خ', new ArabicForm(0xFEA5, 0xFEA7, 0xFEA8, 0xFEA6) },
                { 'د', new ArabicForm(0xFEA9, 0xFEA9, 0xFEAA, 0xFEAA) },
                { 'ذ', new ArabicForm(0xFEAB, 0xFEAB, 0xFEAC, 0xFEAC) },
                { 'ر', new ArabicForm(0xFEAD, 0xFEAD, 0xFEAE, 0xFEAE) },
                { 'ز', new ArabicForm(0xFEAF, 0xFEAF, 0xFEB0, 0xFEB0) },
                { 'س', new ArabicForm(0xFEB1, 0xFEB3, 0xFEB4, 0xFEB2) },
                { 'ش', new ArabicForm(0xFEB5, 0xFEB7, 0xFEB8, 0xFEB6) },
                { 'ص', new ArabicForm(0xFEB9, 0xFEBB, 0xFEBC, 0xFEBA) },
                { 'ض', new ArabicForm(0xFEBD, 0xFEBF, 0xFEC0, 0xFEBE) },
                { 'ط', new ArabicForm(0xFEC1, 0xFEC3, 0xFEC4, 0xFEC2) },
                { 'ظ', new ArabicForm(0xFEC5, 0xFEC7, 0xFEC8, 0xFEC6) },
                { 'ع', new ArabicForm(0xFEC9, 0xFECB, 0xFECC, 0xFECA) },
                { 'غ', new ArabicForm(0xFECD, 0xFECF, 0xFED0, 0xFECE) },
                { 'ف', new ArabicForm(0xFED1, 0xFED3, 0xFED4, 0xFED2) },
                { 'ق', new ArabicForm(0xFED5, 0xFED7, 0xFED8, 0xFED6) },
                { 'ك', new ArabicForm(0xFED9, 0xFEDB, 0xFEDC, 0xFEDA) },
                { 'ل', new ArabicForm(0xFEDD, 0xFEDF, 0xFEE0, 0xFEDE) },
                { 'م', new ArabicForm(0xFEE1, 0xFEE3, 0xFEE4, 0xFEE2) },
                { 'ن', new ArabicForm(0xFEE5, 0xFEE7, 0xFEE8, 0xFEE6) },
                { 'ه', new ArabicForm(0xFEE9, 0xFEEB, 0xFEEC, 0xFEEA) },
                { 'و', new ArabicForm(0xFEED, 0xFEED, 0xFEEE, 0xFEEE) },
                { 'ى', new ArabicForm(0xFEEF, 0xFEEF, 0xFEF0, 0xFEF0) },
                { 'ي', new ArabicForm(0xFEF1, 0xFEF3, 0xFEF4, 0xFEF2) },
            };

        /// <summary>
        /// الحروف التي لا تتصل بما يليها من الجهة اليسرى.
        /// تأخذ هذه الحروف فقط شكلَي Isolated أو Final.
        /// </summary>
        private static readonly HashSet<char> NonConnectingNext = new HashSet<char>
        {
            'ا', 'أ', 'إ', 'آ',
            'د', 'ذ',
            'ر', 'ز',
            'و', 'ؤ',
            'ة', 'ى', 'ء'
        };

        // ─────────────────────────────────────────────
        // الدالة الرئيسية
        // ─────────────────────────────────────────────

        /// <summary>
        /// يحوّل النص إلى شكله المرئي الصحيح لعرضه في PDF.
        /// </summary>
        public static string Fix(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var segments = SplitIntoSegments(input);
            var result = new StringBuilder(input.Length + 8);

            for (int s = segments.Count - 1; s >= 0; s--)
            {
                var (text, isArabic) = segments[s];

                if (isArabic)
                {
                    string shaped = ShapeArabicSegment(text);
                    char[] chars = shaped.ToCharArray();
                    Array.Reverse(chars);
                    result.Append(chars);
                }
                else
                {
                    result.Append(text);
                }
            }

            return result.ToString();
        }

        // ─────────────────────────────────────────────
        // تقسيم النص إلى مقاطع
        // ─────────────────────────────────────────────

        private static List<(string text, bool isArabic)> SplitIntoSegments(string input)
        {
            var segments = new List<(string, bool)>(8);
            var current = new StringBuilder(input.Length);

            bool currentIsArabic = false;
            foreach (char c in input)
            {
                if (c != ' ')
                {
                    currentIsArabic = CharMap.ContainsKey(c);
                    break;
                }
            }

            foreach (char c in input)
            {
                bool charIsArabic = (c == ' ')
                    ? currentIsArabic
                    : CharMap.ContainsKey(c);

                if (charIsArabic != currentIsArabic && current.Length > 0)
                {
                    segments.Add((current.ToString(), currentIsArabic));
                    current.Clear();
                    currentIsArabic = charIsArabic;
                }

                current.Append(c);
            }

            if (current.Length > 0)
                segments.Add((current.ToString(), currentIsArabic));

            return segments;
        }

        // ─────────────────────────────────────────────
        // تشكيل الحروف (Contextual Shaping)
        // ─────────────────────────────────────────────

        private static string ShapeArabicSegment(string input)
        {
            var sb = new StringBuilder(input.Length);

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == ' ')
                {
                    sb.Append(c);
                    continue;
                }

                // ── لام-ألف Ligature ──
                if (c == 'ل' && i + 1 < input.Length)
                {
                    int laForm = GetLamAlefForm(input[i + 1]);
                    if (laForm != -1)
                    {
                        bool prevConnects = i > 0 && CanConnectToNext(input[i - 1]);
                        sb.Append((char)(prevConnects ? laForm + 1 : laForm));
                        i++;
                        continue;
                    }
                }

                if (!CharMap.TryGetValue(c, out ArabicForm form))
                {
                    sb.Append(c);
                    continue;
                }

                // هل الحرف السابق يتصل بالحرف الحالي من اليمين؟
                bool connectPrev = i > 0 && CanConnectToNext(input[i - 1]);

                // ★ الإصلاح الجوهري:
                // connectNext = الحرف الحالي قادر على الاتصال بما يليه (ليس في NonConnectingNext)
                //             + الحرف التالي موجود في الجدول
                bool connectNext = i + 1 < input.Length
                    && CanConnectToNext(c)
                    && CanConnectToPrev(input[i + 1]);

                int codePoint;
                if (connectPrev && connectNext) codePoint = form.Medial;
                else if (connectPrev) codePoint = form.Final;
                else if (connectNext) codePoint = form.Initial;
                else codePoint = form.Isolated;

                sb.Append((char)codePoint);
            }

            return sb.ToString();
        }

        // ─────────────────────────────────────────────
        // دوال مساعدة
        // ─────────────────────────────────────────────

        private static int GetLamAlefForm(char next)
        {
            switch (next)
            {
                case 'آ': return 0xFEF5;
                case 'أ': return 0xFEF7;
                case 'إ': return 0xFEF9;
                case 'ا': return 0xFEFB;
                default: return -1;
            }
        }

        /// <summary>يتصل الحرف بما يليه من اليسار؟</summary>
        private static bool CanConnectToNext(char c)
            => CharMap.ContainsKey(c) && !NonConnectingNext.Contains(c);

        /// <summary>يقبل الحرف الاتصال من اليمين؟ (جميع الحروف العربية تقبل)</summary>
        private static bool CanConnectToPrev(char c)
            => CharMap.ContainsKey(c);
    }
}