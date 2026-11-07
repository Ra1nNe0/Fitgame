using System;
using System.Collections.Generic;

namespace FitQuest
{
    public enum ItemKind { Skin, Boost, CoinPack, Premium }

    public class ShopItem
    {
        public string Id;
        public ItemKind Kind;
        public string Title;
        public string Description;
        public string Icon;          // имя иконки или спрайта
        public int CoinPrice;        // цена в монетах (0 — не продаётся за монеты)
        public int RubPrice;         // цена в рублях (демо-оплата), 0 — не продаётся за деньги
        public string Skin;          // для образов
        public int Amount;           // для бустов и пакетов монет
        public bool PremiumOnly;     // только для владельцев Премиума
    }

    /// <summary>
    /// Модель монетизации — freemium:
    ///   • игра бесплатна целиком (все 12 уровней);
    ///   • монеты зарабатываются в игре и тратятся на образы героя и бусты;
    ///   • за деньги — пакеты монет и подписка FitQuest Premium
    ///     (x2 монеты, режим «Марафон», продолжение без рекламы);
    ///   • реклама с вознаграждением: продолжить забег или получить монеты.
    /// В учебном прототипе оплата демонстрационная: деньги не списываются,
    /// но покупка проходит весь путь — выбор, подтверждение, выдача товара, учёт.
    /// </summary>
    public static class Shop
    {
        public const int AdReward = 25;
        public const int AdsPerDay = 3;

        public static readonly ShopItem[] Items =
        {
            Skin("skin_king", "king", "КОРОЛЬ", "Корона, меховой плащ и скипетр", 0),
            Skin("skin_knight", "knight", "РЫЦАРЬ", "Доспехи, корсет и сиреневый плащ", 0),

            new ShopItem { Id = "boost_shield", Kind = ItemKind.Boost, Title = "ЩИТ", Icon = "icon_shield", Amount = 1,
                           CoinPrice = 60, Description = "Защищает от одной ошибки в забеге" },
            new ShopItem { Id = "boost_magnet", Kind = ItemKind.Boost, Title = "МАГНИТ", Icon = "icon_magnet", Amount = 1,
                           CoinPrice = 80, Description = "Двойные монеты в одном забеге" },

            new ShopItem { Id = "coins_500", Kind = ItemKind.CoinPack, Title = "500 МОНЕТ", Icon = "coin_0", Amount = 500, RubPrice = 79,
                           Description = "Небольшой мешочек монет" },
            new ShopItem { Id = "coins_1500", Kind = ItemKind.CoinPack, Title = "1500 МОНЕТ", Icon = "coin_0", Amount = 1500, RubPrice = 199,
                           Description = "Выгоднее на 15%" },
            new ShopItem { Id = "coins_5000", Kind = ItemKind.CoinPack, Title = "5000 МОНЕТ", Icon = "coin_0", Amount = 5000, RubPrice = 449,
                           Description = "Сундук монет: выгоднее на 40%" },

            new ShopItem { Id = "premium", Kind = ItemKind.Premium, Title = "FITQUEST PREMIUM", Icon = "icon_crown", RubPrice = 249,
                           Description = "x2 монеты, Марафон и продолжение без рекламы" },
        };

        static ShopItem Skin(string id, string skin, string title, string description, int price)
        {
            return new ShopItem { Id = id, Kind = ItemKind.Skin, Skin = skin, Title = title, Description = description, CoinPrice = price };
        }

        /// <summary>Образы, которые есть у игрока, по порядку витрины (для выбора персонажа в меню).</summary>
        public static List<ShopItem> OwnedSkins()
        {
            var result = new List<ShopItem>();
            foreach (ShopItem item in Items)
                if (item.Kind == ItemKind.Skin && Owns(item)) result.Add(item);
            return result;
        }

        public static ShopItem Find(string id)
        {
            foreach (ShopItem item in Items)
                if (item.Id == id) return item;
            return null;
        }

        public static IEnumerable<ShopItem> OfKind(ItemKind kind)
        {
            foreach (ShopItem item in Items)
                if (item.Kind == kind) yield return item;
        }

        public static bool Owns(ShopItem item)
        {
            Profile p = SaveData.Current;
            switch (item.Kind)
            {
                case ItemKind.Skin: return p.ownedSkins.Contains(item.Skin) || (item.PremiumOnly && p.premium);
                case ItemKind.Premium: return p.premium;
                default: return false;
            }
        }

        /// <summary>Покупка за монеты. Возвращает текст ошибки или null при успехе.</summary>
        public static string BuyWithCoins(ShopItem item)
        {
            Profile p = SaveData.Current;
            if (item.CoinPrice <= 0) return "Этот товар не продаётся за монеты";
            if (Owns(item)) return "Уже куплено";
            if (p.coins < item.CoinPrice) return "Не хватает монет: нужно ещё " + (item.CoinPrice - p.coins);

            p.coins -= item.CoinPrice;
            Grant(item);
            Log(item, item.CoinPrice + " монет");
            return null;
        }

        /// <summary>Вызывается, когда демо-оплата «прошла».</summary>
        public static void CompleteDemoPayment(ShopItem item)
        {
            Grant(item);
            SaveData.Current.demoRevenue += item.RubPrice;
            Log(item, item.RubPrice + " руб. (демо)");
        }

        static void Grant(ShopItem item)
        {
            Profile p = SaveData.Current;
            switch (item.Kind)
            {
                case ItemKind.Skin:
                    if (!p.ownedSkins.Contains(item.Skin)) p.ownedSkins.Add(item.Skin);
                    p.skin = item.Skin;
                    break;
                case ItemKind.Boost:
                    if (item.Id == "boost_shield") p.shields += item.Amount;
                    else p.magnets += item.Amount;
                    break;
                case ItemKind.CoinPack:
                    p.coins += item.Amount;
                    break;
                case ItemKind.Premium:
                    p.premium = true;
                    break;
            }
            SaveData.Save();
            Achievements.Evaluate();
        }

        static void Log(ShopItem item, string price)
        {
            SaveData.Current.purchases.Add(new PurchaseRecord
            {
                item = item.Title,
                price = price,
                date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });
            SaveData.Save();
        }

        /// <summary>Сколько рекламных роликов за монеты ещё можно посмотреть сегодня.</summary>
        public static int AdsLeftToday
        {
            get
            {
                Profile p = SaveData.Current;
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                if (p.adDay != today) { p.adDay = today; p.adsToday = 0; }
                return Math.Max(0, AdsPerDay - p.adsToday);
            }
        }

        public static void RewardAdWatched(bool forCoins)
        {
            Profile p = SaveData.Current;
            p.adsWatched++;
            if (forCoins)
            {
                p.adsToday++;
                p.coins += AdReward;
            }
            SaveData.Save();
        }

        public static string SkinTitle(string skin)
        {
            foreach (ShopItem item in Items)
                if (item.Kind == ItemKind.Skin && item.Skin == skin) return item.Title;
            return skin;
        }
    }
}
