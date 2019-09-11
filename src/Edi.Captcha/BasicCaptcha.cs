using System;
using System.Text;

namespace Edi.Captcha
{
    public class BasicLetterCaptcha : SessionBasedCaptcha
    {
        public string Letters { get; set; }

        public int CodeLength { get; }

        public BasicLetterCaptcha(
            string letters = "2346789ABCDEFGHJKLMNPRTUVWXYZ",
            string sessionName = "CaptchaCode",
            int codeLength = 4)
        {
            Letters = letters;
            SessionName = sessionName;
            if (codeLength < 1 || codeLength > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(codeLength),
                    $"codeLength must range within 1-32, current value is {codeLength}");
            }
            CodeLength = codeLength;
        }

        public override string GenerateCaptchaCode()
        {
            var rand = new Random();
            var maxRand = Letters.Length - 1;

            var sb = new StringBuilder();
            for (var i = 0; i < CodeLength; i++)
            {
                var index = rand.Next(maxRand);
                sb.Append(Letters[index]);
            }

            return sb.ToString();
        }
    }
}
