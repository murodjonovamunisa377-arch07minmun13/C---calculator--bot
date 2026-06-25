// See https://aka.ms/new-console-template for more information
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using NCalc;

class Program
{
    
    private static readonly string BotToken = "8915815070:AAHQk-WOW8X4yTlzimMMMsjbxsJTtRgBS9I";

    static async Task Main(string[] args)
    {
        if (BotToken == "your_bot_token_here")
        {
            Console.WriteLine("Iltimos, avval 'BotToken' o'zgaruvchisiga haqiqiy Telegram API tokenni kiriting!");
            return;
        }

        var botClient = new TelegramBotClient(BotToken);
        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"@{me.Username} muvaffaqiyatli ishga tushdi!");
        Console.WriteLine("To'xtatish uchun Enter tugmasini bosing...");
        Console.ReadLine();

        cts.Cancel();
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { Text: { } messageText } message)
            return;

        long chatId = message.Chat.Id;

        if (messageText.StartsWith("/start"))
        {
            string welcomeText = "Salom! Men matematik ifodalarni hisoblovchi botman.\n\n" +
                                 "Menga istalgan matematik ifodani yuboring, masalan:\n" +
                                 "`2 + 3.5 * 4`\n" +
                                 "`(10 - 2) / 2`\n" +
                                 "`2 ^ 3` yoki `(5 + 5) ^ (2 * 1.5)`";
            
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: welcomeText,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
            return;
        }

        Console.WriteLine($"[Xabar keldi] ChatId: {chatId}, Ifoda: {messageText}");

        string responseText;
        try
        {
            // Matndagi bo'shliqlarni olib tashlaymiz va ifodani tayyorlaymiz
            string cleanExpression = messageText.Replace(" ", "");
            string formattedExpression = ConvertPowerToPow(cleanExpression);

            Expression expr = new Expression(formattedExpression);
            
            // Sonlarni avtomatik double (o'nli kasr) sifatida ishlashi uchun sozlaymiz
            expr.Parameters["Pow"] = new Func<double, double, double>(Math.Pow);

            object result = expr.Evaluate();

            if (result != null)
            {
                // Agar natija butun son bo'lsa (.0) qismini chiroyli formatlaymiz
                double finalResult = Convert.ToDouble(result);
                responseText = $"Natija: *{finalResult:0.######}*";
            }
            else
            {
                responseText = "Ifodani hisoblab bo'lmadi.";
            }
        }
        catch (Exception)
        {
            responseText = "Xatolik! Ifoda noto'g'ri shakllantirilgan.\n" +
                           "Faqat +, -, *, /, ^, qavslar va sonlardan foydalaning.";
        }

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: responseText,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Rekursiv ravishda matndagi barcha 'a^b' ifodalarni 'Pow(a, b)' ko'rinishiga o'giradi.
    /// Qavslar ichidagi murakkab ifodalarni ham to'g'ri tahlil qiladi.
    /// </summary>
    private static string ConvertPowerToPow(string input)
    {
        while (input.Contains("^"))
        {
            // Regex orqali darajaning chap (asos) va o'ng (daraja) tomonini qidiramiz
            // Bu qolip oddiy sonlarni ham, qavs ichidagi ifodalarni ham qamrab oladi
            int caretIndex = input.IndexOf('^');
            if (caretIndex == -1) break;

            string left = FindOperand(input, caretIndex, isLeft: true);
            string right = FindOperand(input, caretIndex, isLeft: false);

            string originalSegment = left + "^" + right;
            string replacedSegment = $"Pow({left},{right})";

            input = input.Replace(originalSegment, replacedSegment);
        }
        return input;
    }

    private static string FindOperand(string expr, int caretIndex, bool isLeft)
    {
        int index = isLeft ? caretIndex - 1 : caretIndex + 1;
        int step = isLeft ? -1 : 1;
        int bracketCount = 0;
        string result = "";

        while (index >= 0 && index < expr.Length)
        {
            char c = expr[index];

            if (c == (isLeft ? ')' : '(')) bracketCount++;
            else if (c == (isLeft ? '(' : ')')) bracketCount--;

            if (bracketCount == 0)
            {
                // Agar qavsdan tashqarida operatorga duch kelsak, to'xtaymiz
                if (isLeft && (c == '+' || c == '-' || c == '*' || c == '/' || c == ',')) break;
                if (!isLeft && (c == '+' || c == '-' || c == '*' || c == '/' || c == '^' || c == ',')) break;
            }

            if (isLeft) result = c + result;
            else result += c;

            index += step;
            if (bracketCount < 0) break;
        }
        return result;
    }

    private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Xatolik:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}





