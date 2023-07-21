using Newtonsoft.Json.Linq;
using System;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Extensions;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.VisualBasic;
using Telegram.Bot.Types.ReplyMarkups;
using System.Linq;
using System.Collections.Generic;
using TelegramBot.Model;
using System.Reactive.Concurrency;
using Aspose.Pdf;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

class Program
{
    private static string botToken = "5847870640:AAFxdl1u88ccXtgSseC--B9JABx0NK0_JJs";
    private static ITelegramBotClient bot = new TelegramBotClient(botToken);
    private const string FirebaseDatabaseUrl = "https://resume-crafter-39b94-default-rtdb.europe-west1.firebasedatabase.app/";
    private static readonly FirebaseClient firebaseClient = new FirebaseClient(FirebaseDatabaseUrl);
    private enum STATE_OF_PROCESS {IS_START, IS_CITY, IS_EMAIL, IS_PHONE, IS_PURPOSE, IS_EDUCATION, IS_EXPIRIENCE, IS_SKILLS, IS_SKILLS_CHOOSE, IS_END };
    private static int State { get; set; } = 0;
    public static UserProfile userProfile = new UserProfile();
    private static bool IsResumeCreated = false;
    private static bool IsCheckInfo = false;
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("{" + update.Type + "} ");
        Console.ResetColor();
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));

        if (update.Type == UpdateType.Message)
        {
            var msg = update.Message;
            if (msg.Text.ToLower() == "/start")
            {
                var replyKeyboardMain = new ReplyKeyboardMarkup(
                   new KeyboardButton[][]
                   {
                        new KeyboardButton[] { new KeyboardButton("Создать резюме") },
                        new KeyboardButton[] { new KeyboardButton("Моя информация") },
                        new KeyboardButton[] { new KeyboardButton("Создать резюме (PDF)") }
                   }
                );
                replyKeyboardMain.ResizeKeyboard = true;

                await botClient.SendTextMessageAsync(msg.Chat, "Здравствуйте! Я удобный бот для создания резюме. " +
                    "Благодаря мне Вы сможете быстро, легко и удобно создать своё резюме. Выберете один из предложенных" +
                    " вариантов.", 
                    replyMarkup: replyKeyboardMain);
                return;
            }
            else if (msg.Text.ToLower() == "моя информация" || IsCheckInfo)
            {
                IsCheckInfo = false;
                var user = await firebaseClient.Child("users").Child(msg.From.Username).OnceSingleAsync<UserProfile>();
                if(user == null) 
                {
                    await botClient.SendTextMessageAsync(msg.Chat, "❎ У вас ещё нет резюме!");
                    return;
                }
                var replyKeyboardMain = new ReplyKeyboardMarkup(
                   new KeyboardButton[][]
                   {
                        new KeyboardButton[] { new KeyboardButton("Назад") },
                        new KeyboardButton[] { new KeyboardButton("Изменить ФИО") },
                        new KeyboardButton[] { new KeyboardButton("Изменить город") },
                        new KeyboardButton[] { new KeyboardButton("Изменить email") },
                        new KeyboardButton[] { new KeyboardButton("Изменить номер телефона") },
                        new KeyboardButton[] { new KeyboardButton("Изменить цель поиска работы") },
                        new KeyboardButton[] { new KeyboardButton("Изменить информацию об образовании") },
                        new KeyboardButton[] { new KeyboardButton("Изменить информацию об опыте работы") },
                        new KeyboardButton[] { new KeyboardButton("Изменить информацию о навыках") },
                   }
                );
                replyKeyboardMain.ResizeKeyboard = true;
                
                string text = "ℹ Вот Ваша ифнормация:\n" +
                    "ФИО: " + user.Fio + "\nГород: " + user.City + "\nEmail: " + user.Email +
                    "\nТелефон: " + user.Phone + "\nЦель поиска работы: " + user.Purpose +
                    "\nИнформаци об образовании: " + user.Education + "\nИнформация об опыте работы: " + user.Experience +
                    "\nНавыки: ";
                for (int i = 0; i < user.Skills.Count; i++)
                    text += "\t" + (i+1) + ". " + user.Skills[i] + "; \n";                               

                await botClient.SendTextMessageAsync(msg.Chat, text,
                    replyMarkup: replyKeyboardMain);
            }
            else if (msg.Text.ToLower() == "создать резюме" || State != 0)
            {
                List<string> skills = new List<string>();
                IsResumeCreated = false;
                switch((STATE_OF_PROCESS)State)
                {
                    case STATE_OF_PROCESS.IS_START:
                        userProfile.Telegram = msg.From.Username;
                        Message messageFio = null;
                        var replyKeyboardFio = new ReplyKeyboardMarkup(
                            new KeyboardButton[][]
                            {
                                new KeyboardButton[] { new KeyboardButton("Пропустить") }
                            }
                        );
                        replyKeyboardFio.ResizeKeyboard = true;

                        await botClient.SendTextMessageAsync(update.Message.Chat, "Давайте представимся! Введите Ваше ФИО:",
                            replyMarkup: replyKeyboardFio);

                        while (messageFio == null)
                        {
                            var newUpdates = await botClient.GetUpdatesAsync(offset: update.Id + 1, cancellationToken: cancellationToken);
                            for (int i = 0; i < newUpdates.Length; i++)
                                if (newUpdates[i].Type == UpdateType.Message)
                                {
                                    messageFio = newUpdates[i].Message;
                                }
                        }

                        if (messageFio.Text.ToLower() == "пропустить")
                        {
                            State++;
                            return;
                        }
                        userProfile.Fio = messageFio.Text;
                        await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил ФИО");
                        State++;
                        break;

                    case STATE_OF_PROCESS.IS_CITY:
                        Message messageCity = null;
                        var replyKeyboard = new ReplyKeyboardMarkup(
                            new KeyboardButton[][]
                            {
                                new KeyboardButton[] { new KeyboardButton("Пропустить") }
                            }
                        );
                        replyKeyboard.ResizeKeyboard = true;

                        await botClient.SendTextMessageAsync(update.Message.Chat, "Введите город:",
                            replyMarkup: replyKeyboard);
                        while (messageCity == null)
                        {
                            var newUpdates = await botClient.GetUpdatesAsync(offset: update.Id + 1, cancellationToken: cancellationToken);
                            for (int i = 0; i < newUpdates.Length; i++)
                                if (newUpdates[i].Type == UpdateType.Message)
                                {
                                    messageCity = newUpdates[i].Message;
                                }
                        }
                        if (messageCity.Text.ToLower() == "пропустить")
                        {
                            State++;
                            return;
                        }
                        userProfile.City = messageCity.Text;
                        await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил город");
                        State++;
                        break;

                    case STATE_OF_PROCESS.IS_EMAIL:
                        Message messageEmail = null;
                        var replyKeyboardEmail = new ReplyKeyboardMarkup(
                            new KeyboardButton[][]
                            {
                                new KeyboardButton[] { new KeyboardButton("Пропустить") }
                            }
                        );
                        replyKeyboardEmail.ResizeKeyboard = true;

                        await botClient.SendTextMessageAsync(update.Message.Chat, "Введите Ваш email:",
                            replyMarkup: replyKeyboardEmail);
                        while (messageEmail == null)
                        {
                            var newUpdates = await botClient.GetUpdatesAsync(offset: update.Id + 1, cancellationToken: cancellationToken);
                            for (int i = 0; i < newUpdates.Length; i++)
                                if (newUpdates[i].Type == UpdateType.Message)
                                {
                                    messageEmail = newUpdates[i].Message;
                                }
                        }
                        if (messageEmail.Text.ToLower() == "пропустить")
                        {
                            State++;
                            return;
                        }
                        userProfile.Email = messageEmail.Text;
                        await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил email");
                        State++;
                        break;

                    case STATE_OF_PROCESS.IS_PHONE:
                        Message messagePhone = null;
                        var replyKeyboardPhone = new ReplyKeyboardMarkup(
                            new KeyboardButton[][]
                            {
                                new KeyboardButton[] { new KeyboardButton("Пропустить") }
                            }
                        );
                        replyKeyboardPhone.ResizeKeyboard = true;

                        await botClient.SendTextMessageAsync(update.Message.Chat, "Введите Ваш номер телефона:",
                            replyMarkup: replyKeyboardPhone);
                        while (messagePhone == null)
                        {
                            var newUpdates = await botClient.GetUpdatesAsync(offset: update.Id + 1, cancellationToken: cancellationToken);
                            for (int i = 0; i < newUpdates.Length; i++)
                                if (newUpdates[i].Type == UpdateType.Message)
                                {
                                    messagePhone = newUpdates[i].Message;
                                }
                        }
                        if (messagePhone.Text.ToLower() == "пропустить")
                        {
                            State++;
                            return;
                        }

                        userProfile.Phone = messagePhone.Text;
                        await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил номер телефона");
                        State++;
                        break;

                    case STATE_OF_PROCESS.IS_PURPOSE:
                        Message messagePurpose = null;
                        var replyKeyboardPurpose = new ReplyKeyboardMarkup(
                            new KeyboardButton[][]
                            {
                                new KeyboardButton[] { new KeyboardButton("Пропустить") }
                            }
                        );
                        replyKeyboardPurpose.ResizeKeyboard = true;

                        await botClient.SendTextMessageAsync(update.Message.Chat, "Введите Вашу цель поиска работы:",
                            replyMarkup: replyKeyboardPurpose);
                        while (messagePurpose == null)
                        {
                            var newUpdates = await botClient.GetUpdatesAsync(offset: update.Id + 1, cancellationToken: cancellationToken);
                            for (int i = 0; i < newUpdates.Length; i++)
                                if (newUpdates[i].Type == UpdateType.Message)
                                {
                                    messagePurpose = newUpdates[i].Message;
                                }
                        }
                        if (messagePurpose.Text.ToLower() == "пропустить")
                        {
                            State++;
                            return;
                        }

                        userProfile.Purpose = messagePurpose.Text;
                        await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил цель поиска работы");
                        State++; 
                        break;

                    case STATE_OF_PROCESS.IS_EDUCATION:
                        Message messageEducation = null;
                        var replyKeyboardEducation = new ReplyKeyboardMarkup(
                            new KeyboardButton[][]
                            {
                                new KeyboardButton[] { new KeyboardButton("Пропустить") }
                            }
                        );
                        replyKeyboardEducation.ResizeKeyboard = true;

                        await botClient.SendTextMessageAsync(update.Message.Chat, "Введите информацию о Вашем образовании:",
                            replyMarkup: replyKeyboardEducation);
                        while (messageEducation == null)
                        {
                            var newUpdates = await botClient.GetUpdatesAsync(offset: update.Id + 1, cancellationToken: cancellationToken);
                            for (int i = 0; i < newUpdates.Length; i++)
                                if (newUpdates[i].Type == UpdateType.Message)
                                {
                                    messageEducation = newUpdates[i].Message;
                                }
                        }
                        if (messageEducation.Text.ToLower() == "пропустить")
                        {
                            State++;
                            return;
                        }

                        userProfile.Education = messageEducation.Text;
                        await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил инофрмацию об образовании");
                        State++;
                        break;

                    case STATE_OF_PROCESS.IS_EXPIRIENCE:
                        Message messageExpirience = null;
                        var replyKeyboardExpirience = new ReplyKeyboardMarkup(
                            new KeyboardButton[][]
                            {
                                new KeyboardButton[] { new KeyboardButton("Пропустить") }
                            }
                        );
                        replyKeyboardExpirience.ResizeKeyboard = true;

                        await botClient.SendTextMessageAsync(update.Message.Chat, "Введите информацию о Вашем опыте работы:",
                            replyMarkup: replyKeyboardExpirience);
                        while (messageExpirience == null)
                        {
                            var newUpdates = await botClient.GetUpdatesAsync(offset: update.Id + 1, cancellationToken: cancellationToken);
                            for (int i = 0; i < newUpdates.Length; i++)
                                if (newUpdates[i].Type == UpdateType.Message)
                                {
                                    messageExpirience = newUpdates[i].Message;
                                }
                        }
                        if (messageExpirience.Text.ToLower() == "пропустить")
                        {
                            State++;
                            return;
                        }

                        userProfile.Experience = messageExpirience.Text;
                        await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил инофрмацию об опыте работы");
                        State++;
                        break;

                    case STATE_OF_PROCESS.IS_SKILLS:
                        var replyKeyboardSkills = new ReplyKeyboardMarkup(
                            new KeyboardButton[][]
                            {
                                new KeyboardButton[] { new KeyboardButton("Пропустить") },
                                new KeyboardButton[] { new KeyboardButton("Ответственность") },
                                new KeyboardButton[] { new KeyboardButton("Гибкость") },
                                new KeyboardButton[] { new KeyboardButton("Коммуникабельность") },
                                new KeyboardButton[] { new KeyboardButton("Стоп") },
                            }
                        );
                        replyKeyboardSkills.ResizeKeyboard = true;
                        
                        State++;
                        await botClient.SendTextMessageAsync(update.Message.Chat, "Перечислите Ваши навыки. Если вы закончили напишите \"Стоп\" (Возможна задержка в 1-2 минуты):",
                            replyMarkup: replyKeyboardSkills);

                        break;

                    case STATE_OF_PROCESS.IS_SKILLS_CHOOSE:
                        skills.Add(msg.Text);

                        if (msg.Text.ToLower() == "пропустить")
                        {
                            State++;
                            return;
                        }
                        else if (msg.Text.ToLower() == "стоп")
                        {
                            for (int j = 0; j < skills.Count; j++)
                            {
                                userProfile.Skills.Add(skills[j]);
                            }
                            State++;
                            await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил инофрмацию о навыках");
                            return;
                        }                                                 
                        break;

                    case STATE_OF_PROCESS.IS_END:
                        SaveToDatabase(userProfile);

                        await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил резюме", replyMarkup: null);
                        State = 0;
                        IsResumeCreated = true;
                        break;
                }                             
                return;
            }
            else if (msg.Text.ToLower() == "изменить информацию о навыках")
            {
                Message message = null;
                await botClient.SendTextMessageAsync(update.Message.Chat, "Добавьте новый навык. Если вы хотите удалить существующий то напишите \"-\" перед навыком(Например: -Лень):");
                while (message == null)
                {
                    var newUpdates = await botClient.GetUpdatesAsync(offset: update.Id + 1, cancellationToken: cancellationToken);
                    for (int i = 0; i < newUpdates.Length; i++)
                        if (newUpdates[i].Type == UpdateType.Message)
                        {
                            message = newUpdates[i].Message;
                        }
                }

                var dbUser = await firebaseClient.Child("users").Child(msg.From.Username).OnceSingleAsync<UserProfile>();
                UserProfile tempUser = new UserProfile()
                {
                    Fio = dbUser.Fio,
                    Telegram = dbUser.Telegram,
                    Phone = dbUser.Phone,
                    City = dbUser.City,
                    Education = dbUser.Education,
                    Email = dbUser.Email,
                    Purpose = dbUser.Purpose,
                    Experience = dbUser.Experience,
                };

                bool isRemove = false;
                string skill = "";
                if (message.Text[0] == '-')
                {
                    isRemove = true;
                    for (int i = 1; i < message.Text.Length; i++)
                    {
                        skill += message.Text[i];
                    }
                }
                else skill = message.Text;

                if (isRemove)
                    for (int i = 0; i < dbUser.Skills.Count; i++)
                    {
                        if (skill.ToLower() == dbUser.Skills[i].ToLower())
                        {
                            dbUser.Skills.RemoveAt(i);
                            break;
                        }
                    }
                else
                {
                    dbUser.Skills.Add(skill);
                }

                for (int i = 0; i < dbUser.Skills.Count; i++) tempUser.Skills.Add(dbUser.Skills[i]);

                SaveToDatabase(tempUser);
                await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил инофрмацию о навыках:");
                IsCheckInfo = true;
            }
            else if (msg.Text.ToLower() == "изменить фио")
            {

                Message messageFio = null;
                await botClient.SendTextMessageAsync(update.Message.Chat, "Введите новое ФИО:");
                while (messageFio == null)
                {
                    var newUpdates = await botClient.GetUpdatesAsync(offset: update.Id + 1, cancellationToken: cancellationToken);
                    for (int i = 0; i < newUpdates.Length; i++)
                        if (newUpdates[i].Type == UpdateType.Message)
                        {
                            messageFio = newUpdates[i].Message;
                        }
                }

                var dbUser = await firebaseClient.Child("users").Child(msg.From.Username).OnceSingleAsync<UserProfile>();
                UserProfile tempUser = new UserProfile()
                {
                    City = dbUser.City,
                    Telegram = dbUser.Telegram,
                    Phone = dbUser.Phone,
                    Purpose = dbUser.Purpose,
                    Education = dbUser.Education,
                    Email = dbUser.Email,
                    Experience = dbUser.Experience,
                    Fio = messageFio.Text,
                };

                for(int i = 0; i < dbUser.Skills.Count; i++) tempUser.Skills.Add(dbUser.Skills[i]);                

                SaveToDatabase(tempUser);
                await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил ФИО");
                IsCheckInfo = true;
            }
            else if (msg.Text.ToLower() == "изменить город")
            {
                Message message = null;
                await botClient.SendTextMessageAsync(update.Message.Chat, "Введите новый город:");
                while (message == null)
                {
                    var newUpdates = await botClient.GetUpdatesAsync(offset: update.Id + 1, cancellationToken: cancellationToken);
                    for (int i = 0; i < newUpdates.Length; i++)
                        if (newUpdates[i].Type == UpdateType.Message)
                        {
                            message = newUpdates[i].Message;
                        }
                }

                var dbUser = await firebaseClient.Child("users").Child(msg.From.Username).OnceSingleAsync<UserProfile>();
                UserProfile tempUser = new UserProfile()
                {
                    Fio = dbUser.Fio,
                    Telegram = dbUser.Telegram,
                    Phone = dbUser.Phone,
                    Purpose = dbUser.Purpose,
                    Education = dbUser.Education,
                    Email = dbUser.Email,
                    Experience = dbUser.Experience,
                    City = message.Text,
                };

                for (int i = 0; i < dbUser.Skills.Count; i++) tempUser.Skills.Add(dbUser.Skills[i]);

                SaveToDatabase(tempUser);
                await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил город");
                IsCheckInfo = true;
            }
            else if (msg.Text.ToLower() == "изменить email")
            {
                Message message = null;
                await botClient.SendTextMessageAsync(update.Message.Chat, "Введите новый email:");
                while (message == null)
                {
                    var newUpdates = await botClient.GetUpdatesAsync(offset: update.Id + 1, cancellationToken: cancellationToken);
                    for (int i = 0; i < newUpdates.Length; i++)
                        if (newUpdates[i].Type == UpdateType.Message)
                        {
                            message = newUpdates[i].Message;
                        }
                }

                var dbUser = await firebaseClient.Child("users").Child(msg.From.Username).OnceSingleAsync<UserProfile>();
                UserProfile tempUser = new UserProfile()
                {
                    Fio = dbUser.Fio,
                    Telegram = dbUser.Telegram,
                    Phone = dbUser.Phone,
                    Purpose = dbUser.Purpose,
                    Education = dbUser.Education,
                    City = dbUser.City,
                    Experience = dbUser.Experience,
                    Email = message.Text,
                };

                for (int i = 0; i < dbUser.Skills.Count; i++) tempUser.Skills.Add(dbUser.Skills[i]);

                SaveToDatabase(tempUser);
                await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил email");
                IsCheckInfo = true;
            }
            else if (msg.Text.ToLower() == "изменить номер телефона")
            {
                Message message = null;
                await botClient.SendTextMessageAsync(update.Message.Chat, "Введите новый номер телефона:");
                while (message == null)
                {
                    var newUpdates = await botClient.GetUpdatesAsync(offset: update.Id + 1, cancellationToken: cancellationToken);
                    for (int i = 0; i < newUpdates.Length; i++)
                        if (newUpdates[i].Type == UpdateType.Message)
                        {
                            message = newUpdates[i].Message;
                        }
                }

                var dbUser = await firebaseClient.Child("users").Child(msg.From.Username).OnceSingleAsync<UserProfile>();
                UserProfile tempUser = new UserProfile()
                {
                    Fio = dbUser.Fio,
                    Telegram = dbUser.Telegram,
                    City = dbUser.City,
                    Purpose = dbUser.Purpose,
                    Education = dbUser.Education,
                    Email = dbUser.Email,
                    Experience = dbUser.Experience,
                    Phone = message.Text,
                };

                for (int i = 0; i < dbUser.Skills.Count; i++) tempUser.Skills.Add(dbUser.Skills[i]);

                SaveToDatabase(tempUser);
                await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил номер телефона");
                IsCheckInfo = true;
            }
            else if (msg.Text.ToLower() == "изменить цель поиска работы")
            {
                Message message = null;
                await botClient.SendTextMessageAsync(update.Message.Chat, "Введите новую цель поиска работы:");
                while (message == null)
                {
                    var newUpdates = await botClient.GetUpdatesAsync(offset: update.Id + 1, cancellationToken: cancellationToken);
                    for (int i = 0; i < newUpdates.Length; i++)
                        if (newUpdates[i].Type == UpdateType.Message)
                        {
                            message = newUpdates[i].Message;
                        }
                }

                var dbUser = await firebaseClient.Child("users").Child(msg.From.Username).OnceSingleAsync<UserProfile>();
                UserProfile tempUser = new UserProfile()
                {
                    Fio = dbUser.Fio,
                    Telegram = dbUser.Telegram,
                    Phone = dbUser.Phone,
                    City = dbUser.City,
                    Education = dbUser.Education,
                    Email = dbUser.Email,
                    Experience = dbUser.Experience,
                    Purpose = message.Text,
                };

                for (int i = 0; i < dbUser.Skills.Count; i++) tempUser.Skills.Add(dbUser.Skills[i]);

                SaveToDatabase(tempUser);
                await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил цель поиска работы");
                IsCheckInfo = true;
            }
            else if (msg.Text.ToLower() == "изменить информацию об образовании")
            {
                Message message = null;
                await botClient.SendTextMessageAsync(update.Message.Chat, "Введите новую информацию об образовании:");
                while (message == null)
                {
                    var newUpdates = await botClient.GetUpdatesAsync(offset: update.Id + 1, cancellationToken: cancellationToken);
                    for (int i = 0; i < newUpdates.Length; i++)
                        if (newUpdates[i].Type == UpdateType.Message)
                        {
                            message = newUpdates[i].Message;
                        }
                }

                var dbUser = await firebaseClient.Child("users").Child(msg.From.Username).OnceSingleAsync<UserProfile>();
                UserProfile tempUser = new UserProfile()
                {
                    Fio = dbUser.Fio,
                    Telegram = dbUser.Telegram,
                    Phone = dbUser.Phone,
                    City = dbUser.City,
                    Purpose = dbUser.Purpose,
                    Email = dbUser.Email,
                    Experience = dbUser.Experience,
                    Education = message.Text,
                };

                for (int i = 0; i < dbUser.Skills.Count; i++) tempUser.Skills.Add(dbUser.Skills[i]);

                SaveToDatabase(tempUser);
                await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил информацию об образовании");
                IsCheckInfo = true;
            }
            else if (msg.Text.ToLower() == "изменить информацию об опыте работы")
            {
                Message message = null;
                await botClient.SendTextMessageAsync(update.Message.Chat, "Введите новую инофрмацию об опыте работы:");
                while (message == null)
                {
                    var newUpdates = await botClient.GetUpdatesAsync(offset: update.Id + 1, cancellationToken: cancellationToken);
                    for (int i = 0; i < newUpdates.Length; i++)
                        if (newUpdates[i].Type == UpdateType.Message)
                        {
                            message = newUpdates[i].Message;
                        }
                }

                var dbUser = await firebaseClient.Child("users").Child(msg.From.Username).OnceSingleAsync<UserProfile>();
                UserProfile tempUser = new UserProfile()
                {
                    Fio = dbUser.Fio,
                    Telegram = dbUser.Telegram,
                    Phone = dbUser.Phone,
                    City = dbUser.City,
                    Education = dbUser.Education,
                    Email = dbUser.Email,
                    Purpose = dbUser.Purpose,
                    Experience = message.Text,
                };

                for (int i = 0; i < dbUser.Skills.Count; i++) tempUser.Skills.Add(dbUser.Skills[i]);

                SaveToDatabase(tempUser);
                await botClient.SendTextMessageAsync(update.Message.Chat, "✅ Сохранил инофрмацию об опыте работы:");
                IsCheckInfo = true;
            }
            else if (msg.Text.ToLower() == "создать резюме (pdf)")
            {
                //Код создания PDF файла. !!!Не забыть сделать!!!
            }    
            else if (msg.Text.ToLower() == "назад" || IsResumeCreated == true)
            {
                var replyKeyboardMain = new ReplyKeyboardMarkup(
                   new KeyboardButton[][]
                   {
                        new KeyboardButton[] { new KeyboardButton("Создать резюме") },
                        new KeyboardButton[] { new KeyboardButton("Моя информация") },
                        new KeyboardButton[] { new KeyboardButton("Создать резюме (PDF)") }
                   }
                );
                replyKeyboardMain.ResizeKeyboard = true;

                await botClient.SendTextMessageAsync(msg.Chat, "Главная страница",
                    replyMarkup: replyKeyboardMain);
            }
        }
    }

    /// <summary>
    /// Сохраение в базу данных Firebase
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="key"></param>
    public static async void SaveToDatabase(Message msg, string key)
    {
        var data = new JObject();
        data[key] = msg.Text;

        await firebaseClient
            .Child("users")
            .Child(msg.From.Username)
            .PostAsync(data);
    }
    /// <summary>
    /// Сохраение в базу данных Firebase
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="key"></param>
    public static async void SaveToDatabase(string value, string key, string username)
    {
        var data = new JObject();
        data[key] = value;

        await firebaseClient
            .Child("users")
            .Child(username).
            PostAsync(data);
    }
    /// <summary>
    /// Сохраение в базу данных Firebase данных о Telegram
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="key"></param>
    public static async void SaveToDatabase(Message msg)
    {
        var data = new JObject();
        data["telegram"] = msg.From.Username;

        await firebaseClient
            .Child("users")
            .Child(msg.From.Username)
            .PutAsync(data);
    }
    /// <summary>
    /// Сохраение в базу данных Firebase данные объекта UserProfile
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="key"></param>
    public static async void SaveToDatabase(UserProfile up)
    {
        await firebaseClient
            .Child("users")
            .Child(up.Telegram)
            .PutAsync<UserProfile>(up);
    }
    public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("{1Error} ");
        Console.ResetColor();
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
    }
    static void Main(string[] args)
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Bot started " + bot.GetMeAsync().Result.FirstName + "\n");
            Console.ResetColor();

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { },
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.ReadLine();
        }
        catch(Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("\n{2Error} ");
            Console.ResetColor();
            Console.Write(e.Message + "\n");
        }
    }
}