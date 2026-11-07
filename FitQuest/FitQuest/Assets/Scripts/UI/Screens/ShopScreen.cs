using UnityEngine;
using UnityEngine.UI;

namespace FitQuest
{
    /// <summary>
    /// Магазин — механизм монетизации в прототипе: образы и бусты за монеты,
    /// пакеты монет и Премиум за деньги (демо-оплата), монеты за просмотр рекламы.
    /// </summary>
    public class ShopScreen : UiScreen
    {
        RectTransform content;
        Text coins;
        Button[] tabs = new Button[4];
        Button adButton;
        ItemKind tab = ItemKind.Skin;

        /// <summary>Куда вернуться по кнопке «Назад» (по умолчанию — меню).</summary>
        public UiScreen ReturnTo;

        static readonly string[] TabNames = { "ОБРАЗЫ", "БУСТЫ", "МОНЕТЫ", "ПРЕМИУМ" };

        protected override void Build()
        {
            Ui.Dim(Root, 0.5f).raycastTarget = false;
            Ui.Label(Root, "МАГАЗИН", 48, Ui.Gold, new Vector2(-600f, 460f), new Vector2(600f, 70f));
            Ui.Icon(Root, "coin_0", new Vector2(520f, 462f), 64f);
            coins = Ui.Label(Root, "", 40, Ui.Gold, new Vector2(740f, 460f), new Vector2(360f, 60f), TextAnchor.MiddleLeft);

            for (int i = 0; i < 4; i++)
            {
                ItemKind kind = (ItemKind)i;
                tabs[i] = Ui.Button(Root, TabNames[i], new Vector2(-660f + i * 440f, 350f), new Vector2(410f, 90f), Ui.Steel,
                                    () => { tab = kind; Refresh(); }, 24);
            }

            content = Ui.Node(Root, "Items", new Vector2(0f, -40f), new Vector2(1840f, 620f));
            Ui.Button(Root, "НАЗАД", new Vector2(-640f, -470f), new Vector2(400f, 96f), Ui.Slate, Back, 24, "icon_back");
            adButton = Ui.Button(Root, "", new Vector2(420f, -470f), new Vector2(860f, 96f), Ui.Magenta, WatchAd, 20, "icon_film");
        }

        public void SetTab(ItemKind kind)
        {
            tab = kind;
        }

        public override void OnShow()
        {
            Refresh();
            Music.Play("menu");
        }

        public override void OnHide()
        {
            ReturnTo = null;
        }

        void Back()
        {
            App.Go(ReturnTo ?? App.Menu);
        }

        public override bool OnBack()
        {
            Back();
            return true;
        }

        public void Refresh()
        {
            Profile p = SaveData.Current;
            coins.text = p.coins.ToString();
            for (int i = 0; i < 4; i++) Ui.SetButtonColor(tabs[i], (ItemKind)i == tab ? Ui.Amber : Ui.Steel);
            int left = Shop.AdsLeftToday;
            Ui.SetButtonText(adButton, left > 0 ? "РЕКЛАМА: +" + Shop.AdReward + " МОНЕТ (ОСТАЛОСЬ " + left + ")" : "РЕКЛАМА: ЗАВТРА СНОВА");
            Ui.SetEnabled(adButton, left > 0);

            Ui.Clear(content);
            var items = new System.Collections.Generic.List<ShopItem>(Shop.OfKind(tab));
            float width = items.Count >= 7 ? 250f : (items.Count >= 5 ? 350f : 420f);
            float step = width + 20f;
            float start = -(items.Count - 1) * step / 2f;
            for (int i = 0; i < items.Count; i++)
                Card(items[i], new Vector2(start + i * step, 0f), width);

            if (tab == ItemKind.Premium) PremiumInfo();
        }

        void Card(ShopItem item, Vector2 position, float width)
        {
            Profile p = SaveData.Current;
            bool premiumTab = item.Kind == ItemKind.Premium;
            RectTransform card = Ui.Panel(content, position, new Vector2(width, 600f), premiumTab).rectTransform;
            Color titleColor = premiumTab ? Ui.Ink : Color.white;

            if (item.Kind == ItemKind.Skin)
                Ui.Animated(card, Art.Hero(item.Skin, "idle", 2), 2f, new Vector2(0f, 202f), 6f);
            else
                Ui.Icon(card, item.Icon, new Vector2(0f, 160f), 144f);

            Text title = Ui.Label(card, item.Title, 24, item.PremiumOnly ? Ui.Gold : titleColor, new Vector2(0f, -10f), new Vector2(width - 30f, 40f));
            Text desc = Ui.Label(card, item.Description, 16, premiumTab ? Ui.Ink : Ui.Silver, new Vector2(0f, -100f), new Vector2(width - 40f, 120f));
            if (premiumTab) { title.GetComponent<Shadow>().enabled = false; desc.GetComponent<Shadow>().enabled = false; }

            if (item.Kind == ItemKind.Boost)
            {
                int have = item.Id == "boost_shield" ? p.shields : p.magnets;
                Ui.Label(card, "ЕСТЬ: " + have, 16, Ui.Cyan, new Vector2(0f, -175f), new Vector2(width - 40f, 30f));
            }

            string label;
            Color color;
            bool enabled = true;
            System.Action action;

            if (item.Kind == ItemKind.Skin && Shop.Owns(item))
            {
                bool selected = p.skin == item.Skin;
                label = selected ? "ВЫБРАН" : "ВЫБРАТЬ";
                color = selected ? Ui.Steel : Ui.Blue;
                enabled = !selected;
                action = () =>
                {
                    if (!p.ownedSkins.Contains(item.Skin)) p.ownedSkins.Add(item.Skin);
                    p.skin = item.Skin;
                    SaveData.Save();
                    App.Player.SetSkin(item.Skin);
                    Refresh();
                };
            }
            else if (item.Kind == ItemKind.Premium && p.premium)
            {
                label = "АКТИВЕН";
                color = Ui.Steel;
                enabled = false;
                action = null;
            }
            else if (item.PremiumOnly)
            {
                label = "НУЖЕН ПРЕМИУМ";
                color = Ui.Gold;
                action = () => { tab = ItemKind.Premium; Refresh(); };
            }
            else if (item.CoinPrice > 0)
            {
                label = item.CoinPrice + " МОНЕТ";
                color = p.coins >= item.CoinPrice ? Ui.Green : Ui.Steel;
                action = () => BuyCoins(item);
            }
            else
            {
                label = item.RubPrice + " РУБ." + (premiumTab ? "/МЕС" : "");
                color = Ui.Green;
                action = () => App.BuyForMoney(item, Refresh);
            }

            Button b = Ui.Button(card, label, new Vector2(0f, -245f), new Vector2(width - 40f, 90f), color, action, 16);
            Ui.SetEnabled(b, enabled);
        }

        void PremiumInfo()
        {
            string text =
                "ЧТО ДАЁТ ПРЕМИУМ:\n" +
                "• x2 монеты за каждый забег\n" +
                "• режим МАРАФОН с таблицей рекордов\n" +
                "• продолжение забега без рекламы";
            RectTransform info = Ui.Panel(content, new Vector2(-500f, 0f), new Vector2(760f, 600f)).rectTransform;
            Ui.Label(info, text, 20, Color.white, new Vector2(0f, 60f), new Vector2(700f, 360f), TextAnchor.MiddleLeft);
            Ui.Label(info, "Оплата в прототипе демонстрационная:\nденьги не списываются.", 16, Ui.Gray,
                     new Vector2(0f, -220f), new Vector2(700f, 80f));
            // карточку самого Премиума сдвигаем вправо
            if (content.childCount > 0)
                content.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(420f, 0f);
        }

        void BuyCoins(ShopItem item)
        {
            string error = Shop.BuyWithCoins(item);
            if (error != null)
            {
                App.Modal.Show("НЕ ПОЛУЧИЛОСЬ", error, "ПОНЯТНО", null);
                Sfx.Play("weak");
            }
            else
            {
                Sfx.Play("buy");
                App.Toasts.Push("ПОКУПКА", item.Title, item.Kind == ItemKind.Skin ? "icon_cart" : item.Icon);
                if (item.Kind == ItemKind.Skin) App.Player.SetSkin(item.Skin);
            }
            Refresh();
        }

        void WatchAd()
        {
            if (Shop.AdsLeftToday <= 0) return;
            UiScreen back = this;
            App.ShowAd(true, () =>
            {
                App.Toasts.Push("НАГРАДА", "+" + Shop.AdReward + " МОНЕТ", "coin_0");
                Sfx.Play("coin");
                App.Go(back);
            });
        }
    }
}
