using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using Object = StardewValley.Object;

namespace SaveAnywhere.Framework {
    public class NewShippingMenuV2 : IClickableMenu {
        public const int region_okbutton = 101;
        public const int region_forwardButton = 102;
        public const int region_backButton = 103;
        public const int farming_category = 0;
        public const int foraging_category = 1;
        public const int fishing_category = 2;
        public const int mining_category = 3;
        public const int other_category = 4;
        public const int total_category = 5;
        public const int timePerIntroCategory = 500;
        public const int outroFadeTime = 800;
        public const int smokeRate = 100;
        public const int categorylabelHeight = 25;
        protected bool _hasFinished;
        public List<TemporaryAnimatedSprite> animations = new();
        public ClickableTextureComponent backButton;
        public List<ClickableTextureComponent> categories = new();
        private readonly List<MoneyDial> categoryDials = new();
        private readonly List<List<Item>> categoryItems = new();
        private readonly int categoryLabelsWidth;
        private readonly List<int> categoryTotals = new();
        private int centerX;
        private int centerY;
        public int currentPage = -1;
        public int currentTab;
        private int dayPlaqueY;
        private int finalOutroTimer;
        public ClickableTextureComponent forwardButton;
        private int introTimer = 3500;
        private readonly int itemAndPlusButtonWidth;
        private readonly int itemSlotWidth;
        public int itemsPerCategoryPage = 9;
        private readonly Dictionary<Item, int> itemValues = new();
        private int moonShake = -1;
        private bool newDayPlaque;
        public ClickableTextureComponent okButton;
        private bool outro;
        private int outroFadeTimer;
        private int outroPauseBeforeDateChange;
        private readonly int plusButtonWidth;
        private bool savedYet;
        private int smokeTimer;
        private int timesPokedMoon;
        private readonly int totalWidth;
        private float weatherX;
        


        public NewShippingMenuV2(IList<Item> items) : base(0, 0, Game1.viewport.Width, Game1.viewport.Height) {
            Game1.player.team.endOfNightStatus.UpdateState("shipment");
            parseItems(items);
            if (!Game1.wasRainingYesterday)
                Game1.changeMusicTrack(Game1.currentSeason.Equals("summer") ? "nightTime" : "none");
            categoryLabelsWidth = 512;
            plusButtonWidth = 40;
            itemSlotWidth = 96;
            itemAndPlusButtonWidth = plusButtonWidth + itemSlotWidth + 8;
            totalWidth = categoryLabelsWidth + itemAndPlusButtonWidth;
            centerX = Game1.viewport.Width / 2;
            centerY = Game1.viewport.Height / 2;
            _hasFinished = false;
            outro = true;
            var num = -1;
            for (var index = 0; index < 6; ++index) {
                var categories = this.categories;
                var textureComponent = new ClickableTextureComponent("",
                    new Rectangle(centerX + totalWidth / 2 - plusButtonWidth, centerY - 300 + index * 27 * 4,
                        plusButtonWidth, 44), "", getCategoryName(index), Game1.mouseCursors,
                    new Rectangle(392, 361, 10, 11), 4f);
                textureComponent.visible = index < 5 && categoryItems[index].Count > 0;
                textureComponent.myID = index;
                textureComponent.downNeighborID = index < 4 ? index + 1 : 101;
                textureComponent.upNeighborID = index > 0 ? num : -1;
                textureComponent.upNeighborImmutable = true;
                categories.Add(textureComponent);
                num = index >= 5 || categoryItems[index].Count <= 0 ? num : index;
            }

            dayPlaqueY = this.categories[0].bounds.Y - 128;
            var rectangle = new Rectangle(centerX + totalWidth / 2 - itemAndPlusButtonWidth + 32, centerY + 300 - 64,
                64, 64);
            var textureComponent1 = new ClickableTextureComponent(
                Game1.content.LoadString("Strings\\StringsFromCSFiles:ShippingMenu.cs.11382"), rectangle, null,
                Game1.content.LoadString("Strings\\StringsFromCSFiles:ShippingMenu.cs.11382"), Game1.mouseCursors,
                new Rectangle(128, 256, 64, 64), 1f);
            textureComponent1.myID = 101;
            textureComponent1.upNeighborID = num;
            okButton = textureComponent1;
            if (Game1.options.gamepadControls) {
                Mouse.SetPosition(rectangle.Center.X, rectangle.Center.Y);
                Game1.InvalidateOldMouseMovement();
                Game1.lastCursorMotionWasMouse = false;
            }

            var textureComponent2 = new ClickableTextureComponent("",
                new Rectangle(xPositionOnScreen + 32, yPositionOnScreen + height - 64, 48, 44), null, "",
                Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f);
            textureComponent2.myID = 103;
            textureComponent2.rightNeighborID = -7777;
            backButton = textureComponent2;
            var textureComponent3 = new ClickableTextureComponent("",
                new Rectangle(xPositionOnScreen + width - 32 - 48, yPositionOnScreen + height - 64, 48, 44), null, "",
                Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f);
            textureComponent3.myID = 102;
            textureComponent3.leftNeighborID = 103;
            forwardButton = textureComponent3;
            if (Game1.dayOfMonth == 25 && Game1.currentSeason.Equals("winter"))
                animations.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(640, 800, 32, 16),
                    80f, 2, 1000, new Vector2(Game1.viewport.Width, Game1.random.Next(0, 200)), false, false, 0.01f,
                    0.0f, Color.White, 4f, 0.0f, 0.0f, 0.0f, true) {
                    motion = new Vector2(-4f, 0.0f),
                    delayBeforeAnimationStart = 3000
                });
            Game1.stats.checkForShippingAchievements();
            if (!Game1.player.achievements.Contains(34) && Utility.hasFarmerShippedAllItems())
                Game1.getAchievement(34);
            RepositionItems();
            populateClickableComponentList();
            if (!Game1.options.SnappyMenus)
                return;
            base.snapToDefaultClickableComponent();
        }

        public void RepositionItems() {
            centerX = Game1.viewport.Width / 2;
            centerY = Game1.viewport.Height / 2;
            for (var index = 0; index < 6; ++index)
                categories[index].bounds = new Rectangle(centerX + totalWidth / 2 - plusButtonWidth,
                    centerY - 300 + index * 27 * 4, plusButtonWidth, 44);
            dayPlaqueY = categories[0].bounds.Y - 128;
            if (dayPlaqueY < 0)
                dayPlaqueY = -64;
            backButton.bounds.X = xPositionOnScreen + 32;
            backButton.bounds.Y = yPositionOnScreen + height - 64;
            forwardButton.bounds.X = xPositionOnScreen + width - 32 - 48;
            forwardButton.bounds.Y = yPositionOnScreen + height - 64;
            okButton.bounds = new Rectangle(centerX + totalWidth / 2 - itemAndPlusButtonWidth + 32, centerY + 300 - 64,
                64, 64);
            itemsPerCategoryPage = (int) ((yPositionOnScreen + height - 64 - (yPositionOnScreen + 32)) / 68.0);
            if (currentPage < 0)
                return;
            currentTab = Utility.Clamp(currentTab, 0, (categoryItems[currentPage].Count - 1) / itemsPerCategoryPage);
        }

        protected override void customSnapBehavior(int direction, int oldRegion, int oldID) {
            if (oldID != 103 || direction != 1 || !showForwardButton())
                return;
            currentlySnappedComponent = getComponentWithID(102);
            snapCursorToCurrentSnappedComponent();
        }

        public override void snapToDefaultClickableComponent() {
            if (currentPage != -1)
                currentlySnappedComponent = getComponentWithID(103);
            else
                currentlySnappedComponent = getComponentWithID(101);
            snapCursorToCurrentSnappedComponent();
        }

        public void parseItems(IList<Item> items) {
            Utility.consolidateStacks(items);
            for (var index = 0; index < 6; ++index) {
                categoryItems.Add(new List<Item>());
                categoryTotals.Add(0);
                categoryDials.Add(new MoneyDial(7, index == 5));
            }

            foreach (var key in items)
                if (key is Object) {
                    var o = key as Object;
                    var categoryIndexForObject = getCategoryIndexForObject(o);
                    categoryItems[categoryIndexForObject].Add(o);
                    var num = o.sellToStorePrice() * o.Stack;
                    categoryTotals[categoryIndexForObject] += num;
                    itemValues[key] = num;
                    Game1.stats.itemsShipped += (uint) o.Stack;
                    if (o.Category == -75 || o.Category == -79)
                        Game1.stats.CropsShipped += (uint) o.Stack;
                    if (o.countsForShippedCollection())
                        Game1.player.shippedBasic(o.ParentSheetIndex, o.Stack);
                }

            for (var index = 0; index < 5; ++index) {
                categoryTotals[5] += categoryTotals[index];
                categoryItems[5].AddRange(categoryItems[index]);
                categoryDials[index].currentValue = categoryTotals[index];
                categoryDials[index].previousTargetValue = categoryDials[index].currentValue;
            }

            categoryDials[5].currentValue = categoryTotals[5];
            Game1.setRichPresence("earnings", categoryTotals[5]);
        }

        public int getCategoryIndexForObject(Object o) {
            switch (o.ParentSheetIndex) {
                case 296:
                case 396:
                case 402:
                case 406:
                case 410:
                case 414:
                case 418:
                    return 1;
                default:
                    switch (o.Category) {
                        case -81:
                        case -27:
                        case -23:
                            return 1;
                        case -80:
                        case -79:
                        case -75:
                        case -26:
                        case -14:
                        case -6:
                        case -5:
                            return 0;
                        case -20:
                        case -4:
                            return 2;
                        case -15:
                        case -12:
                        case -2:
                            return 3;
                        default:
                            return 4;
                    }
            }
        }

        public string getCategoryName(int index) {
            switch (index) {
                case 0:
                    return Game1.content.LoadString("Strings\\StringsFromCSFiles:ShippingMenu.cs.11389");
                case 1:
                    return Game1.content.LoadString("Strings\\StringsFromCSFiles:ShippingMenu.cs.11390");
                case 2:
                    return Game1.content.LoadString("Strings\\StringsFromCSFiles:ShippingMenu.cs.11391");
                case 3:
                    return Game1.content.LoadString("Strings\\StringsFromCSFiles:ShippingMenu.cs.11392");
                case 4:
                    return Game1.content.LoadString("Strings\\StringsFromCSFiles:ShippingMenu.cs.11393");
                case 5:
                    return Game1.content.LoadString("Strings\\StringsFromCSFiles:ShippingMenu.cs.11394");
                default:
                    return "";
            }
        }

        public override void update(GameTime time) {
            base.update(time);
            if (_hasFinished) {
                shipItems();
                exitThisMenu(false);
            }
            else {
                double weatherX = this.weatherX;
                var elapsedGameTime = time.ElapsedGameTime;
                var num1 = elapsedGameTime.Milliseconds * 0.029999999329447746;
                this.weatherX = (float) (weatherX + num1);
                for (var index = animations.Count - 1; index >= 0; --index)
                    if (animations[index].update(time))
                        animations.RemoveAt(index);
                if (outro) {
                    if (this.outroFadeTimer > 0) {
                        var outroFadeTimer = this.outroFadeTimer;
                        elapsedGameTime = time.ElapsedGameTime;
                        var milliseconds = elapsedGameTime.Milliseconds;
                        this.outroFadeTimer = outroFadeTimer - milliseconds;
                    }
                    else if (this.outroFadeTimer <= 0 && this.dayPlaqueY < centerY - 64) {
                        if (animations.Count > 0)
                            animations.Clear();
                        var dayPlaqueY = this.dayPlaqueY;
                        elapsedGameTime = time.ElapsedGameTime;
                        var num2 = (int) Math.Ceiling(elapsedGameTime.Milliseconds * 0.349999994039536);
                        this.dayPlaqueY = dayPlaqueY + num2;
                        if (this.dayPlaqueY >= centerY - 64)
                            outroPauseBeforeDateChange = 700;
                    }
                    else if (outroPauseBeforeDateChange > 0) {
                        var beforeDateChange = outroPauseBeforeDateChange;
                        elapsedGameTime = time.ElapsedGameTime;
                        var milliseconds = elapsedGameTime.Milliseconds;
                        outroPauseBeforeDateChange = beforeDateChange - milliseconds;
                        if (outroPauseBeforeDateChange <= 0) {
                            Game1.playSound("newRecipe");
                            finalOutroTimer = 2000;
                            animations.Clear();
                            if (!savedYet)
                                savedYet = true;
                        }
                    }
                    else if (this.finalOutroTimer > 0 && savedYet) {
                        var finalOutroTimer = this.finalOutroTimer;
                        elapsedGameTime = time.ElapsedGameTime;
                        var milliseconds = elapsedGameTime.Milliseconds;
                        this.finalOutroTimer = finalOutroTimer - milliseconds;
                        if (this.finalOutroTimer <= 0)
                            _hasFinished = true;
                    }
                }

                if (introTimer >= 0) {
                    var introTimer1 = introTimer;
                    var introTimer2 = introTimer;
                    elapsedGameTime = time.ElapsedGameTime;
                    var num3 = elapsedGameTime.Milliseconds *
                               (Game1.oldMouseState.LeftButton == ButtonState.Pressed ? 3 : 1);
                    introTimer = introTimer2 - num3;
                    if (introTimer1 % 500 < introTimer % 500 && introTimer <= 3000) {
                        var num4 = 4 - introTimer / 500;
                        if (num4 < 6 && num4 > -1) {
                            if (categoryItems[num4].Count > 0) {
                                Game1.playSound(getCategorySound(num4));
                                categoryDials[num4].currentValue = 0;
                                categoryDials[num4].previousTargetValue = 0;
                            }
                            else {
                                Game1.playSound("stoneStep");
                            }
                        }
                    }

                    if (introTimer < 0) {
                        if (Game1.options.SnappyMenus)
                            base.snapToDefaultClickableComponent();
                        Game1.playSound("money");
                        categoryDials[5].currentValue = 0;
                        categoryDials[5].previousTargetValue = 0;
                    }
                }
                else if (Game1.dayOfMonth != 28 && !outro) {
                    if (!Game1.wasRainingYesterday) {
                        var vector2 = new Vector2(Game1.viewport.Width, Game1.random.Next(200));
                        var rectangle = new Rectangle(640, 752, 16, 16);
                        var num5 = Game1.random.Next(1, 4);
                        if (Game1.random.NextDouble() < 0.001) {
                            var flag = Game1.random.NextDouble() < 0.5;
                            if (Game1.random.NextDouble() < 0.5)
                                animations.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors",
                                    new Rectangle(640, 826, 16, 8), 40f, 4, 0,
                                    new Vector2(Game1.random.Next(centerX * 2), Game1.random.Next(centerY)), false,
                                    flag) {
                                    rotation = 3.141593f,
                                    scale = 4f,
                                    motion = new Vector2(flag ? -8f : 8f, 8f),
                                    local = true
                                });
                            else
                                animations.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors",
                                    new Rectangle(258, 1680, 16, 16), 40f, 4, 0,
                                    new Vector2(Game1.random.Next(centerX * 2), Game1.random.Next(centerY)), false,
                                    flag) {
                                    scale = 4f,
                                    motion = new Vector2(flag ? -8f : 8f, 8f),
                                    local = true
                                });
                        }
                        else if (Game1.random.NextDouble() < 0.0002) {
                            vector2 = new Vector2(Game1.viewport.Width, Game1.random.Next(4, 256));
                            animations.Add(new TemporaryAnimatedSprite("", new Rectangle(0, 0, 1, 1), 9999f, 1, 10000,
                                vector2, false, false, 0.01f, 0.0f,
                                Color.White * (0.25f + (float) Game1.random.NextDouble()), 4f, 0.0f, 0.0f, 0.0f, true) {
                                motion = new Vector2(-0.25f, 0.0f)
                            });
                        }
                        else if (Game1.random.NextDouble() < 5E-05) {
                            vector2 = new Vector2(Game1.viewport.Width, Game1.viewport.Height - 192);
                            for (var index = 0; index < num5; ++index) {
                                animations.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", rectangle,
                                    Game1.random.Next(60, 101), 4, 100,
                                    vector2 + new Vector2((index + 1) * Game1.random.Next(15, 18), (index + 1) * -20),
                                    false, false, 0.01f, 0.0f, Color.Black, 4f, 0.0f, 0.0f, 0.0f, true) {
                                    motion = new Vector2(-1f, 0.0f)
                                });
                                animations.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", rectangle,
                                    Game1.random.Next(60, 101), 4, 100,
                                    vector2 + new Vector2((index + 1) * Game1.random.Next(15, 18), (index + 1) * 20),
                                    false, false, 0.01f, 0.0f, Color.Black, 4f, 0.0f, 0.0f, 0.0f, true) {
                                    motion = new Vector2(-1f, 0.0f)
                                });
                            }
                        }
                        else if (Game1.random.NextDouble() < 1E-05) {
                            rectangle = new Rectangle(640, 784, 16, 16);
                            animations.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", rectangle, 75f, 4, 1000,
                                vector2, false, false, 0.01f, 0.0f, Color.White, 4f, 0.0f, 0.0f, 0.0f, true) {
                                motion = new Vector2(-3f, 0.0f),
                                yPeriodic = true,
                                yPeriodicLoopTime = 1000f,
                                yPeriodicRange = 8f,
                                shakeIntensity = 0.5f
                            });
                        }
                    }

                    var smokeTimer = this.smokeTimer;
                    elapsedGameTime = time.ElapsedGameTime;
                    var milliseconds = elapsedGameTime.Milliseconds;
                    this.smokeTimer = smokeTimer - milliseconds;
                    if (this.smokeTimer <= 0) {
                        this.smokeTimer = 50;
                        animations.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors",
                            new Rectangle(684, 1075, 1, 1), 1000f, 1, 1000,
                            new Vector2(188f, Game1.viewport.Height - 128 + 20), false, false) {
                            color = Game1.wasRainingYesterday ? Color.SlateGray : Color.White,
                            scale = 4f,
                            scaleChange = 0.0f,
                            alphaFade = 1f / 400f,
                            motion = new Vector2(0.0f, (float) (-Game1.random.Next(25, 75) / 100.0 / 4.0)),
                            acceleration = new Vector2(-1f / 1000f, 0.0f)
                        });
                    }
                }

                if (this.moonShake <= 0)
                    return;
                var moonShake = this.moonShake;
                elapsedGameTime = time.ElapsedGameTime;
                var milliseconds1 = elapsedGameTime.Milliseconds;
                this.moonShake = moonShake - milliseconds1;
            }
        }

        public string getCategorySound(int which) {
            switch (which) {
                case 0:
                    return !(categoryItems[0][0] as Object).isAnimalProduct() ? "harvest" : "cluck";
                case 1:
                    return "leafrustle";
                case 2:
                    return "button1";
                case 3:
                    return "hammer";
                case 4:
                    return "coin";
                case 5:
                    return "money";
                default:
                    return "stoneStep";
            }
        }

        public override void applyMovementKey(int direction) {
            if (!CanReceiveInput())
                return;
            base.applyMovementKey(direction);
        }

        public override void performHoverAction(int x, int y) {
            if (!CanReceiveInput())
                return;
            base.performHoverAction(x, y);
            if (currentPage == -1) {
                okButton.tryHover(x, y);
                foreach (var category in categories)
                    category.sourceRect.X = !category.containsPoint(x, y) ? 392 : 402;
            }
            else {
                backButton.tryHover(x, y, 0.5f);
                forwardButton.tryHover(x, y, 0.5f);
            }
        }

        public bool CanReceiveInput() {
            return introTimer <= 0 && !outro;
        }

        public override void receiveKeyPress(Keys key) {
            if (!CanReceiveInput())
                return;
            if (introTimer <= 0 && !Game1.options.gamepadControls && (key.Equals((Keys) 27) ||
                                                                      Game1.options.doesInputListContain(
                                                                          Game1.options.menuButton, key))) {
                base.receiveLeftClick(okButton.bounds.Center.X, okButton.bounds.Center.Y);
            }
            else {
                if (introTimer > 0 || (Game1.options.gamepadControls &&
                                       Game1.options.doesInputListContain(Game1.options.menuButton, key)))
                    return;
                base.receiveKeyPress(key);
            }
        }

        public override void receiveGamePadButton(Buttons b) {
            if (!CanReceiveInput())
                return;
            base.receiveGamePadButton(b);
            if (b == Buttons.B && currentPage != -1) {
                if (currentTab == 0) {
                    if (Game1.options.SnappyMenus) {
                        currentlySnappedComponent = getComponentWithID(currentPage);
                        snapCursorToCurrentSnappedComponent();
                    }

                    currentPage = -1;
                }
                else {
                    --currentTab;
                }

                Game1.playSound("shwip");
            }
            else {
                if ((b != Buttons.Start && b != Buttons.B) || currentPage != -1 || outro)
                    return;
                if (introTimer <= 0)
                    okClicked();
                else
                    introTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds * 2;
            }
        }

        private void okClicked() {
            outro = true;
            outroFadeTimer = 800;
            Game1.playSound("bigDeSelect");
            Game1.changeMusicTrack("none");
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true) {
            if (!CanReceiveInput())
                return;
            if (outro && !savedYet) {
                savedYet = true;
            }
            else {
                base.receiveLeftClick(x, y, playSound);
                if (currentPage == -1 && introTimer <= 0 && okButton.containsPoint(x, y))
                    okClicked();
                if (currentPage == -1) {
                    for (var index = 0; index < categories.Count; ++index)
                        if (categories[index].visible && categories[index].containsPoint(x, y)) {
                            currentPage = index;
                            Game1.playSound("shwip");
                            if (Game1.options.SnappyMenus) {
                                currentlySnappedComponent = getComponentWithID(103);
                                snapCursorToCurrentSnappedComponent();
                            }

                            break;
                        }

                    int num;
                    if (Game1.dayOfMonth == 28 && timesPokedMoon <= 10) {
                        var rectangle = new Rectangle(Game1.viewport.Width - 176, 4, 172, 172);
                        num = !rectangle.Contains(x, y) ? 1 : 0;
                    }
                    else {
                        num = 1;
                    }

                    if (num != 0)
                        return;
                    moonShake = 100;
                    ++timesPokedMoon;
                    if (timesPokedMoon > 10)
                        Game1.playSound("shadowDie");
                    else
                        Game1.playSound("thudStep");
                }
                else if (backButton.containsPoint(x, y)) {
                    if (currentTab == 0) {
                        if (Game1.options.SnappyMenus) {
                            currentlySnappedComponent = getComponentWithID(currentPage);
                            snapCursorToCurrentSnappedComponent();
                        }

                        currentPage = -1;
                    }
                    else {
                        --currentTab;
                    }

                    Game1.playSound("shwip");
                }
                else {
                    if (!showForwardButton() || !forwardButton.containsPoint(x, y))
                        return;
                    ++currentTab;
                    Game1.playSound("shwip");
                }
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true) { }

        public bool showForwardButton() {
            return categoryItems[currentPage].Count > itemsPerCategoryPage * (currentTab + 1);
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) {
            initialize(0, 0, Game1.viewport.Width, Game1.viewport.Height);
            RepositionItems();
        }

        public override void draw(SpriteBatch b)
        {
            if (Game1.wasRainingYesterday) {
                b.Draw(Game1.mouseCursors, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height),
                    new Rectangle(639, 858, 1, 184),
                    Game1.currentSeason.Equals("winter")
                        ? Color.LightSlateGray
                        : Color.SlateGray * (float) (1.0 - introTimer / 3500.0));
                b.Draw(Game1.mouseCursors, new Rectangle(2556, 0, Game1.viewport.Width, Game1.viewport.Height),
                    new Rectangle(639, 858, 1, 184),
                    Game1.currentSeason.Equals("winter")
                        ? Color.LightSlateGray
                        : Color.SlateGray * (float) (1.0 - introTimer / 3500.0));
                for (var index = -244; index < Game1.viewport.Width + 244; index += 244)
                    b.Draw(Game1.mouseCursors, new Vector2(index + (float) (weatherX / 2.0 % 244.0), 32f),
                        new Rectangle(643, 1142, 61, 53),
                        Color.DarkSlateGray * 1f * (float) (1.0 - introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, 0, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(0.0f, Game1.viewport.Height - 192),
                    new Rectangle(0, Game1.currentSeason.Equals("winter") ? 1034 : 737, 639, 48),
                    Game1.currentSeason.Equals("winter")
                        ? Color.White * 0.25f
                        : new Color(30, 62, 50) * (float) (0.5 - introTimer / 3500.0), 0.0f, Vector2.Zero, 4f,
                    (SpriteEffects) 1, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(2556f, Game1.viewport.Height - 192),
                    new Rectangle(0, Game1.currentSeason.Equals("winter") ? 1034 : 737, 639, 48),
                    Game1.currentSeason.Equals("winter")
                        ? Color.White * 0.25f
                        : new Color(30, 62, 50) * (float) (0.5 - introTimer / 3500.0), 0.0f, Vector2.Zero, 4f,
                    (SpriteEffects) 1, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(0.0f, Game1.viewport.Height - 128),
                    new Rectangle(0, Game1.currentSeason.Equals("winter") ? 1034 : 737, 639, 32),
                    Game1.currentSeason.Equals("winter")
                        ? Color.White * 0.5f
                        : new Color(30, 62, 50) * (float) (1.0 - introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, 0, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(2556f, Game1.viewport.Height - 128),
                    new Rectangle(0, Game1.currentSeason.Equals("winter") ? 1034 : 737, 639, 32),
                    Game1.currentSeason.Equals("winter")
                        ? Color.White * 0.5f
                        : new Color(30, 62, 50) * (float) (1.0 - introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, 0, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(160f, Game1.viewport.Height - 128 + 16 + 8),
                    new Rectangle(653, 880, 10, 10), Color.White * (float) (1.0 - introTimer / 3500.0), 0.0f,
                    Vector2.Zero, 4f, 0, 1f);
                for (var index = -244; index < Game1.viewport.Width + 244; index += 244)
                    b.Draw(Game1.mouseCursors, new Vector2(index + weatherX % 244f, -32f),
                        new Rectangle(643, 1142, 61, 53), Color.SlateGray * 0.85f * (float) (1.0 - introTimer / 3500.0),
                        0.0f, Vector2.Zero, 4f, 0, 0.9f);
                foreach (var animation in animations)
                    animation.draw(b, true);
                for (var index = -244; index < Game1.viewport.Width + 244; index += 244)
                    b.Draw(Game1.mouseCursors, new Vector2(index + (float) (weatherX * 1.5 % 244.0), sbyte.MinValue),
                        new Rectangle(643, 1142, 61, 53), Color.LightSlateGray * (float) (1.0 - introTimer / 3500.0),
                        0.0f, Vector2.Zero, 4f, 0, 0.9f);
            }
            else {
                b.Draw(Game1.mouseCursors, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height),
                    new Rectangle(639, 858, 1, 184), Color.White * (float) (1.0 - introTimer / 3500.0));
                b.Draw(Game1.mouseCursors, new Rectangle(2556, 0, Game1.viewport.Width, Game1.viewport.Height),
                    new Rectangle(639, 858, 1, 184), Color.White * (float) (1.0 - introTimer / 3500.0));
                b.Draw(Game1.mouseCursors, new Vector2(0.0f, 0.0f), new Rectangle(0, 1453, 639, 195),
                    Color.White * (float) (1.0 - introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, 0, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(2556f, 0.0f), new Rectangle(0, 1453, 639, 195),
                    Color.White * (float) (1.0 - introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, 0, 1f);
                if (Game1.dayOfMonth == 28) {
                    b.Draw(Game1.mouseCursors,
                        new Vector2(Game1.viewport.Width - 176, 4f) + (moonShake > 0
                            ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2))
                            : Vector2.Zero), new Rectangle(642, 835, 43, 43),
                        Color.White * (float) (1.0 - introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, 0, 1f);
                    if (timesPokedMoon > 10) {
                        var spriteBatch = b;
                        var mouseCursors = Game1.mouseCursors;
                        var vector2 = new Vector2(Game1.viewport.Width - 136, 48f) + (moonShake > 0
                            ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2))
                            : Vector2.Zero);
                        int num;
                        if (Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 4000.0 >= 200.0) {
                            var totalGameTime = Game1.currentGameTime.TotalGameTime;
                            if (totalGameTime.TotalMilliseconds % 8000.0 > 7600.0) {
                                totalGameTime = Game1.currentGameTime.TotalGameTime;
                                if (totalGameTime.TotalMilliseconds % 8000.0 < 7800.0)
                                    goto label_21;
                            }

                            num = 0;
                            goto label_22;
                        }

                        label_21:
                        num = 21;
                        label_22:
                        var nullable = new Rectangle(685, 844 + num, 19, 21);
                        var color = Color.White * (float) (1.0 - introTimer / 3500.0);
                        var zero = Vector2.Zero;
                        spriteBatch.Draw(mouseCursors, vector2, nullable, color, 0.0f, zero, 4f, 0, 1f);
                    }
                }

                b.Draw(Game1.mouseCursors, new Vector2(0.0f, Game1.viewport.Height - 192),
                    new Rectangle(0, Game1.currentSeason.Equals("winter") ? 1034 : 737, 639, 48),
                    Game1.currentSeason.Equals("winter")
                        ? Color.White * 0.25f
                        : new Color(0, 20, 40) * (float) (0.649999976158142 - introTimer / 3500.0), 0.0f, Vector2.Zero,
                    4f, (SpriteEffects) 1, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(2556f, Game1.viewport.Height - 192),
                    new Rectangle(0, Game1.currentSeason.Equals("winter") ? 1034 : 737, 639, 48),
                    Game1.currentSeason.Equals("winter")
                        ? Color.White * 0.25f
                        : new Color(0, 20, 40) * (float) (0.649999976158142 - introTimer / 3500.0), 0.0f, Vector2.Zero,
                    4f, (SpriteEffects) 1, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(0.0f, Game1.viewport.Height - 128),
                    new Rectangle(0, Game1.currentSeason.Equals("winter") ? 1034 : 737, 639, 32),
                    Game1.currentSeason.Equals("winter")
                        ? Color.White * 0.5f
                        : new Color(0, 32, 20) * (float) (1.0 - introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, 0, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(2556f, Game1.viewport.Height - 128),
                    new Rectangle(0, Game1.currentSeason.Equals("winter") ? 1034 : 737, 639, 32),
                    Game1.currentSeason.Equals("winter")
                        ? Color.White * 0.5f
                        : new Color(0, 32, 20) * (float) (1.0 - introTimer / 3500.0), 0.0f, Vector2.Zero, 4f, 0, 1f);
                b.Draw(Game1.mouseCursors, new Vector2(160f, Game1.viewport.Height - 128 + 16 + 8),
                    new Rectangle(653, 880, 10, 10), Color.White * (float) (1.0 - introTimer / 3500.0), 0.0f,
                    Vector2.Zero, 4f, 0, 1f);
            }

            if (!outro && !Game1.wasRainingYesterday)
                foreach (var animation in animations)
                    animation.draw(b, true);
            if (currentPage == -1) {
                var num1 = categories[0].bounds.Y - 128;
                if (num1 >= 0)
                    SpriteText.drawStringWithScrollCenteredAt(b, Utility.getDateString(), Game1.viewport.Width / 2,
                        num1);
                var num2 = -20;
                var index1 = 0;
                foreach (var category in categories) {
                    if (introTimer < 2500 - index1 * 500) {
                        var vector2 = category.getVector2() + new Vector2(12f, -8f);
                        if (category.visible) {
                            category.draw(b);
                            b.Draw(Game1.mouseCursors, vector2 + new Vector2(-104f, num2 + 4),
                                new Rectangle(293, 360, 24, 24), Color.White, 0.0f, Vector2.Zero, 4f, 0, 0.88f);
                            categoryItems[index1][0]
                                .drawInMenu(b, vector2 + new Vector2(-88f, num2 + 16), 1f, 1f, 0.9f, 0);
                        }

                        drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
                            (int) (vector2.X + (double) -itemSlotWidth - categoryLabelsWidth - 12.0),
                            (int) (vector2.Y + (double) num2), categoryLabelsWidth, 104, Color.White, 4f, false);
                        SpriteText.drawString(b, category.hoverText,
                            (int) vector2.X - itemSlotWidth - categoryLabelsWidth + 8, (int) vector2.Y + 4);
                        for (var index2 = 0; index2 < 6; ++index2)
                            b.Draw(Game1.mouseCursors,
                                vector2 + new Vector2(-itemSlotWidth - 192 - 24 + index2 * 6 * 4, 12f),
                                new Rectangle(355, 476, 7, 11), Color.White, 0.0f, Vector2.Zero, 4f, 0, 0.88f);
                        categoryDials[index1].draw(b, vector2 + new Vector2(-itemSlotWidth - 192 - 48 + 4, 20f),
                            categoryTotals[index1]);
                        b.Draw(Game1.mouseCursors, vector2 + new Vector2(-itemSlotWidth - 64 - 4, 12f),
                            new Rectangle(408, 476, 9, 11), Color.White, 0.0f, Vector2.Zero, 4f, 0, 0.88f);
                    }

                    ++index1;
                }

                if (introTimer <= 0)
                    okButton.draw(b);
            }
            else {
                drawTextureBox(b, 0, 0, Game1.viewport.Width, Game1.viewport.Height, Color.White);
                var vector2 = new Vector2(xPositionOnScreen + 32, yPositionOnScreen + 32);
                for (var index = currentTab * itemsPerCategoryPage;
                     index < currentTab * itemsPerCategoryPage + itemsPerCategoryPage;
                     ++index)
                    if (categoryItems[currentPage].Count > index) {
                        var key = categoryItems[currentPage][index];
                        key.drawInMenu(b, vector2, 1f, 1f, 1f, (StackDrawType) 1);
                        int stack;
                        if (LocalizedContentManager.CurrentLanguageLatin) {
                            var spriteBatch1 = b;
                            var displayName1 = key.DisplayName;
                            string str1;
                            if (key.Stack <= 1) {
                                str1 = "";
                            }
                            else {
                                stack = key.Stack;
                                str1 = " x" + stack;
                            }

                            var str2 = displayName1 + str1;
                            var num3 = (int) vector2.X + 64 + 12;
                            var num4 = (int) vector2.Y + 12;
                            SpriteText.drawString(spriteBatch1, str2, num3, num4);
                            var str3 = ".";
                            var num5 = 0;
                            while (true) {
                                var num6 = num5;
                                var num7 = width - 96;
                                var displayName2 = key.DisplayName;
                                string str4;
                                if (key.Stack <= 1) {
                                    str4 = "";
                                }
                                else {
                                    stack = key.Stack;
                                    str4 = " x" + stack;
                                }

                                var str5 = Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020",
                                    itemValues[key]);
                                var widthOfString = SpriteText.getWidthOfString(displayName2 + str4 + str5);
                                var num8 = num7 - widthOfString;
                                if (num6 < num8) {
                                    str3 += " .";
                                    num5 += SpriteText.getWidthOfString(" .");
                                }
                                else {
                                    break;
                                }
                            }

                            var spriteBatch2 = b;
                            var str6 = str3;
                            var num9 = (int) vector2.X + 80;
                            var displayName3 = key.DisplayName;
                            string str7;
                            if (key.Stack <= 1) {
                                str7 = "";
                            }
                            else {
                                stack = key.Stack;
                                str7 = " x" + stack;
                            }

                            var widthOfString1 = SpriteText.getWidthOfString(displayName3 + str7);
                            var num10 = num9 + widthOfString1;
                            var num11 = (int) vector2.Y + 8;
                            SpriteText.drawString(spriteBatch2, str6, num10, num11);
                            SpriteText.drawString(b,
                                Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020",
                                    itemValues[key]),
                                (int) vector2.X + width - 64 - SpriteText.getWidthOfString(
                                    Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020",
                                        itemValues[key])), (int) vector2.Y + 12);
                        }
                        else {
                            var displayName = key.DisplayName;
                            string str8;
                            if (key.Stack <= 1) {
                                str8 = ".";
                            }
                            else {
                                stack = key.Stack;
                                str8 = " x" + stack;
                            }

                            var str9 = displayName + str8;
                            var str10 = Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020",
                                itemValues[key]);
                            var num = (int) vector2.X + width - 64 - SpriteText.getWidthOfString(
                                Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020",
                                    itemValues[key]));
                            SpriteText.getWidthOfString(str9 + str10);
                            while (SpriteText.getWidthOfString(str9 + str10) < 1123)
                                str9 += " .";
                            if (SpriteText.getWidthOfString(str9 + str10) >= 1155)
                                str9 = str9.Remove(str9.Length - 1);
                            SpriteText.drawString(b, str9, (int) vector2.X + 64 + 12, (int) vector2.Y + 12);
                            SpriteText.drawString(b, str10, num, (int) vector2.Y + 12);
                        }

                        vector2.Y += 68f;
                    }

                backButton.draw(b);
                if (showForwardButton())
                    forwardButton.draw(b);
            }

            if (outro) {
                b.Draw(Game1.mouseCursors, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height),
                    new Rectangle(639, 858, 1, 184), Color.Black * (1F - outroFadeTimer / 800F));
                SpriteText.drawStringWithScrollCenteredAt(b,
                    newDayPlaque ? Utility.getDateString() : Utility.getDateString(), Game1.viewport.Width / 2,
                    dayPlaqueY);
                foreach (var animation in animations)
                    animation.draw(b, true);
                if (finalOutroTimer > 0 || _hasFinished)
                    b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height),
                        new Rectangle(0, 0, 1, 1), Color.Black * (1F - finalOutroTimer * 0.0005F));
            }

            if (Game1.options.SnappyMenus && (introTimer > 0 || outro))
                return;
            drawMouse(b);
        }

        public void shipItems() {
            var shippingBin = Game1.getFarm().getShippingBin(Game1.player);
            if (Game1.player.useSeparateWallets || (!Game1.player.useSeparateWallets && Game1.player.IsMainPlayer)) {
                var num = 0;
                foreach (var obj in shippingBin)
                    if (obj is Object)
                        num += (obj as Object).sellToStorePrice() * obj.Stack;
                Game1.player.Money += num;
                Game1.getFarm().getShippingBin(Game1.player).Clear();
            }

            if (!Game1.player.useSeparateWallets || !Game1.player.IsMainPlayer)
                return;
            foreach (var allFarmhand in Game1.getAllFarmhands())
                if (!allFarmhand.isActive() && !allFarmhand.isUnclaimedFarmhand) {
                    var num = 0;
                    foreach (var obj in Game1.getFarm().getShippingBin(allFarmhand))
                        if (obj is Object)
                            num += (obj as Object).sellToStorePrice(allFarmhand.UniqueMultiplayerID) * obj.Stack;
                    Game1.player.team.AddIndividualMoney(allFarmhand, num);
                    Game1.getFarm().getShippingBin(allFarmhand).Clear();
                }
        }
    }
}