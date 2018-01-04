﻿using System;
using System.Windows;
using Timer = System.Windows.Forms.Timer;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Sea_Battleship.ShipFolder;
using Core;
using Sea_Battleship.Engine;
using System.Threading;
using Common;
using System.Collections.Generic;

namespace Sea_Battleship
{
    /// <summary>
    /// Логика взаимодействия для PlayField.xaml
    /// </summary>
    public partial class PlayField : UserControl
    {
        private PlayWindow _pw;

        Ships ships;
        private static bool isHiddenField = true;
        private OnlineGame _onlineGame;
        private bool _isOnlineGame;


        public PlayField()
        {
            InitializeComponent();
            Game gg = WindowConfig.game;
            _onlineGame = WindowConfig.OnlineGame;
            CellStatе[,] myArr;
            CellStatе[,] enemyArr;

            if (!(_onlineGame is null))
            {
                _isOnlineGame = true;
                gg = _onlineGame.Game;
                if (_onlineGame.PlayerRole == PlayerRole.Client)
                {
                    myArr = _onlineGame.MyArrangement.GetArrangement();
                    enemyArr = _onlineGame.EnemyArrangement.GetArrangement();
                    ThreadPool.QueueUserWorkItem(OnlineEnemyTurn);
                }
                else
                {
                    myArr = gg.ServerShipArrangement.GetArrangement();
                    enemyArr = gg.ClientShipArrangement.GetArrangement();
                }
            }
            else
            {
                enemyArr = gg.ClientShipArrangement.GetArrangement();
                myArr = gg.ServerShipArrangement.GetArrangement();
            }

            Ships = new Ships(this);
            if (isHiddenField)
            {
                CellStatе[,] arr = enemyArr;
                CellStatе[,] copyarr = new CellStatе[10, 10];
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        copyarr[i, j] = arr[i, j];
                    }
                }
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        Image img = new Image
                        {
                            Stretch = Stretch.Fill,
                            Source = new BitmapImage(new Uri("/Resources/Water.jpg", UriKind.Relative)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache },
                            Opacity = 0
                        };
                        img.MouseLeftButtonDown += FieldCell_Click;
                        FieldGrid.Children.Add(img);
                        Grid.SetColumn(img, x);
                        Grid.SetRow(img, y);
                    }
                }
                if (WindowConfig.IsLoaded)
                {
                    gg.TurnOwner = PlayerRole.Server;
                    PlaceFromMassive(copyarr, Ships, false, true);
                }
                else
                {
                    PlaceFromMassive(copyarr, Ships, false);

                }
            }
            else
            {
                CellStatе[,] arr = myArr;
                CellStatе[,] copyarr = new CellStatе[10, 10];
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        copyarr[i, j] = arr[i, j];
                    }
                }
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        Image img = new Image
                        {
                            Stretch = Stretch.Fill,
                            Source = new BitmapImage(new Uri("/Resources/Water.jpg", UriKind.Relative)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache },
                            Opacity = 0
                        };
                        //img.MouseLeftButtonDown += FieldCell_Click;
                        FieldGrid.Children.Add(img);
                        Grid.SetColumn(img, x);
                        Grid.SetRow(img, y);
                    }

                }
                if (WindowConfig.IsLoaded)
                    PlaceFromMassive(copyarr, Ships, true, true);
                else
                {
                    
                    PlaceFromMassive(copyarr, ships, true);
                }
            }
            isHiddenField = !isHiddenField;
        }

        public void SwitchTurn(bool isFromTimer)
        {
            if (_onlineGame.IsMyTurn)
            {
                if (isFromTimer)
                {
                    _onlineGame.Turn(-1, -1);
                }
                _onlineGame.IsMyTurn = false;
                _onlineGame.IsOne = false;
                WindowConfig.PlayWindowCon.Dispatcher.Invoke(() =>
                {
                    WindowConfig.PlayWindowCon.pr1.Value = 0;
                    WindowConfig.PlayWindowCon.pr1.Visibility = Visibility.Hidden;
                    WindowConfig.PlayWindowCon.MyTurnLabel.Background =
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00287E"));
                    WindowConfig.PlayWindowCon.MyTurnLabel.Content = "Чужой ход";
                });
                ThreadPool.QueueUserWorkItem(OnlineEnemyTurn);
            }
            else
            {
                _onlineGame.IsMyTurn = true;
                WindowConfig.PlayWindowCon.Dispatcher.Invoke(() =>
                {
                    WindowConfig.PlayWindowCon.pr1.Value = 0;
                    WindowConfig.PlayWindowCon.pr1.Visibility = Visibility.Visible;
                    WindowConfig.PlayWindowCon.MyTurnLabel.Background =
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF93FF3A"));
                    WindowConfig.PlayWindowCon.MyTurnLabel.Content = "Ваш ход";
                });
            }
        }


        public Ships Ships { get => ships; set => ships = value; }

        // public bool IsHiddenField { get => isHiddenField; set => isHiddenField = value; }

        public void PlaceShips()
        {
            ships.PlaceAll();
        }

        public static void DeleteCell(int x, int y, PlayField playField)
        {
            Image image = new Image
            {
                Stretch = Stretch.Fill,
                Source = new BitmapImage(new Uri("/Resources/Water.jpg", UriKind.Relative)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache },
                Opacity = 0
            };
            image.MouseLeftButtonDown += playField.FieldCell_Click;
            playField.FieldGrid.Children.RemoveAt(10 * y + x);
            playField.FieldGrid.Children.Insert(10 * y + x, image);
            Grid.SetRow(image, y);
            Grid.SetColumn(image, x);
        }

        public static void SetCell(int x, int y, Grid grid, Image image)
        {
            grid.Children.RemoveAt(10 * y + x);
            grid.Children.Insert(10 * y + x, image);
            Grid.SetRow(image, y);
            Grid.SetColumn(image, x);
        }

        private void SetShotOnField(int x, int y, CellStatе state, bool onMyField)
        {
            _pw = (PlayWindow)((Grid)Parent).Parent;
            var fg = onMyField ? _pw.MyField.FieldGrid : _pw.EnemyField.FieldGrid;
            //string uri = state == CellStatе.WoundedShip ? "/Resources/shipCrushed.png" : "/Resources/waterCrushed.png";
            if (onMyField)
            {
                fg.Dispatcher.Invoke(() =>
                {
                    switch (state)
                    {
                        case CellStatе.WoundedShip:
                            _pw.MyField.Ships.CheckEnemy(new Point(x, y), _pw, true);
                            if(_pw.MyField.Ships.IsAllDead())
                                EndOfGame(false);
                            break;
                        case CellStatе.WoundedWater:
                            SetCell(x, y, fg, new Image()
                            {
                                Stretch = Stretch.Fill,
                                Opacity = 100,
                                Source = new BitmapImage(new Uri("/Resources/waterCrushed.png", UriKind.Relative)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache }
                            });
                            break;
                    }
                });
            }
            else
            {
                fg.Dispatcher.Invoke(() =>
                {
                    switch (state)
                    {
                        case CellStatе.WoundedShip:
                            ships.Check(x, y, _pw, true);
                            if (_pw.MyField.Ships.IsAllDead())
                                EndOfGame(true);
                            break;
                        case CellStatе.WoundedWater:
                            SetCell(x, y, fg, new Image()
                            {
                                Stretch = Stretch.Fill,
                                Opacity = 100,
                                Source = new BitmapImage(new Uri("/Resources/waterCrushed.png", UriKind.Relative)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache }
                            });
                            break;
                    }
                });
            }
        }


        private void OnlineMyTurn(object obj)
        {
            Vector vect = (Vector)obj;
            try
            {
                var shotRes = _onlineGame.Turn((int)vect.X, (int)vect.Y);
                if (shotRes != CellStatе.BlankShot)
                {
                    SetShotOnField((int) vect.X, (int) vect.Y, shotRes, false);
                    if (shotRes == CellStatе.WoundedWater)
                    {
                        SwitchTurn(false);
                    }
                }
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("Соединение разорвано", "Противник отключился", MessageBoxButton.OK, MessageBoxImage.Warning);
                LogService.Trace("Противник отключился");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка соединения", MessageBoxButton.OK, MessageBoxImage.Error);
                LogService.Trace($"Ошибка соединения: {e.Message}");
                OnlineMyTurn(vect);
            }
        }

        private void OnlineEnemyTurn(object obj)
        {
            try
            {
                bool needSwitch = true;
                if (!_onlineGame.IsOne)
                {
                    CellStatе shotRes;
                    _onlineGame.IsOne = true;
                    do
                    {
                        var comeVector = _onlineGame.WaitEnemyTurn();
                        if ((int)comeVector.X == -1 && (int)comeVector.Y == -1)
                        {
                            needSwitch = false;
                            break;
                        }
                        shotRes = _onlineGame.CheckShot(comeVector);
                        SetShotOnField((int)comeVector.X, (int)comeVector.Y, shotRes, true);
                    } while (shotRes == CellStatе.WoundedShip);
                    SwitchTurn(false);
                }
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("Соединение разорвано", "Противник отключился", MessageBoxButton.OK, MessageBoxImage.Warning);
                LogService.Trace("Противник отключился");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка соединения", MessageBoxButton.OK, MessageBoxImage.Error);
                LogService.Trace($"Ошибка соединения: {e.Message}");
                OnlineEnemyTurn(null);
            }
        }

        private void FieldCell_Click(object sender, MouseButtonEventArgs e)
        {
            PlayWindow z = (PlayWindow)((Grid)Parent).Parent;
            Image image = (Image)sender;
            int X = Grid.GetColumn(image);
            int Y = Grid.GetRow(image);
            string uriString = "";
            if (_isOnlineGame && _onlineGame.IsMyTurn)
            {
                ThreadPool.QueueUserWorkItem(OnlineMyTurn, new Vector(X, Y));
            }
            else if (!_isOnlineGame)
            {
                bool res = false;
                MoveResult result;
                do
                {
                    result = z.Game.MakeAMove(X, Y); //ход первого игрока
               
                    switch (result)
                    {
                        case MoveResult.Hit:
                            ships.Check(X, Y, z, false);
                            if (ships.IsAllDead())
                            {
                                EndOfGame(true);
                            }
                            res = false;
                            break;
                        case MoveResult.Miss:
                            uriString = "/Resources/waterCrushed.png";
                            SetCell(Grid.GetColumn(image), Grid.GetRow(image), FieldGrid, new Image()
                            {
                                Stretch = Stretch.Fill,
                                Opacity = 100,
                                Source = new BitmapImage(new Uri(uriString, UriKind.Relative)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache }
                            });
                            res = false;
                            break;

                        case MoveResult.Destroyed:
                            res = true;
                            break;
                        case MoveResult.Error:
                            WindowConfig.game.ChangeTurn();
                            res = true;
                            break;
                    }
                }
                while (res);
                if (result != MoveResult.Hit) //если не попал, ход второго игрока
                {
                    EnemyStep(z);
                }

            }
        }

        public void ChangeTurn(PlayWindow z)
        {
            EnemyStep(z);
        }

        private void EnemyStep(PlayWindow z)
        {
            z.pr1.Value = 0;
            Image image;
            Point p = AI.MakeAMove(z.Game);
            bool was = false;
            do //ходит, пока не промахнётся
            {
                image = (Image)z.MyField.FieldGrid.Children[10 * (int)p.Y + (int)p.X];
                was = z.MyField.ships.Check(image, z, false);
                if (was)
                {
                    p = AI.MakeAMove(z.Game);
                }
                if (z.MyField.ships.IsAllDead())
                {
                    EndOfGame(false);
                    break;
                }

            }
            while (was);
            if (!was)
            {
                SetShotOnField((int)p.X, (int)p.Y, z.MyField.FieldGrid, new Image()
                {
                    Stretch = Stretch.Fill,
                    Opacity = 100,
                    Source = new BitmapImage(new Uri("/Resources/waterCrushed.png", UriKind.Relative)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache }
                });
            }
        }

        private void _SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FieldGrid.Width = FieldGrid.ActualHeight;
        }

        public void PlaceFromMassive(CellStatе[,] cells, Ships ships, bool isPlace)
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (cells[i, j] == CellStatе.Ship|| cells[i, j] == CellStatе.WoundedShip)
                        PluckShip(i, j, cells, ships);
                }
            }
            if (isPlace)
                PlaceShips();
        }

        public void PlaceFromMassive(CellStatе[,] cells, Ships ships, bool isPlace, bool isLoaded)
        {

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (cells[i, j] == CellStatе.Ship || cells[i, j] == CellStatе.WoundedShip || cells[i, j] == CellStatе.DestroyedShip)
                        PluckShip(i, j, cells, ships, isLoaded);
                    else if (cells[i, j] == CellStatе.WoundedWater)
                    {
                        Image img = new Image
                        {
                            Stretch = Stretch.Fill,
                            Source = new BitmapImage(new Uri("/Resources/waterCrushed.png", UriKind.Relative)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache },
                        };
                        //img.MouseLeftButtonDown += FieldCell_Click;
                        FieldGrid.Children.Add(img);
                        Grid.SetColumn(img, i);
                        Grid.SetRow(img, j);
                    }
                }
            }
            if (isPlace)
                PlaceShips();
            foreach(Point p in hitShipList)
            {
                Image img = new Image
                {
                    Stretch = Stretch.Fill,
                    Source = new BitmapImage(new Uri("/Resources/shipCrushed.png", UriKind.Relative)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache },
                };
                FieldGrid.Children.Add(img);
                Grid.SetColumn(img, (int)p.X);
                Grid.SetRow(img, (int)p.Y);
            }
        }

        private void PluckShip(int i, int j, CellStatе[,] cells, Ships Ships)
        {
            int len = 1;
            cells[i, j] = CellStatе.Water;
            if (i + 1 < 10 && cells[i + 1, j] == CellStatе.Ship)
            {
                len++;
                cells[i + 1, j] = CellStatе.Water;
                if (i + 2 < 10 && cells[i + 2, j] == CellStatе.Ship)
                {
                    len++;
                    cells[i + 2, j] = CellStatе.Water;
                    if (i + 3 < 10 && cells[i + 3, j] == CellStatе.Ship)
                    {
                        len++;
                        cells[i + 3, j] = CellStatе.Water;
                        Ships.ShipList4.Add(new Ship4()
                        {
                            IsHorizontal = true,
                            X = i,
                            Y = j,
                            Size = len,
                        });
                    }
                    else
                    {
                        Ships.ShipList3.Add(new Ship3()
                        {
                            IsHorizontal = true,
                            X = i,
                            Y = j,
                            Size = len,
                        });
                    }
                }
                else
                {
                    Ships.ShipList2.Add(new Ship2()
                    {
                        IsHorizontal = true,
                        X = i,
                        Y = j,
                        Size = len,
                    });
                }
            }
            else if (j + 1 < 10 && cells[i, j + 1] == CellStatе.Ship)
            {
                len++;
                cells[i, j + 1] = CellStatе.Water;
                if (j + 2 < 10 && cells[i, j + 2] == CellStatе.Ship)
                {
                    len++;
                    cells[i, j + 2] = CellStatе.Water;
                    if (j + 3 < 10 && cells[i, j + 3] == CellStatе.Ship)
                    {
                        len++;
                        cells[i, j + 3] = CellStatе.Water;
                        Ships.ShipList4.Add(new Ship4()
                        {
                            IsHorizontal = false,
                            X = i,
                            Y = j,
                            Size = len,
                        });
                    }
                    else
                    {
                        Ships.ShipList3.Add(new Ship3()
                        {
                            IsHorizontal = false,
                            X = i,
                            Y = j,
                            Size = len,
                        });
                    }
                }
                else
                {
                    Ships.ShipList2.Add(new Ship2()
                    {
                        IsHorizontal = false,
                        X = i,
                        Y = j,
                        Size = len,
                    });
                }
            }
            else
            {
                Ships.ShipList1.Add(new Ship1()
                {
                    IsHorizontal = true,
                    X = i,
                    Y = j,
                    Size = len,
                });
            }
        }

        List<Point> hitShipList = new List<Point>();

        private void PluckShip(int i, int j, CellStatе[,] cells, Ships Ships, bool isLoaded)
        {
            bool isDead = false;
            int countHit = 0;
            if (cells[i, j] == CellStatе.WoundedShip|| cells[i, j] == CellStatе.DestroyedShip)
            {
                hitShipList.Add(new Point(i, j));
                countHit++;
            }
            int len = 1;
            cells[i, j] = CellStatе.Water;
            if (i + 1 < 10 && (cells[i + 1, j] == CellStatе.Ship|| cells[i + 1, j] == CellStatе.WoundedShip || cells[i+1, j] == CellStatе.DestroyedShip))
            {
                if (cells[i+1, j] == CellStatе.WoundedShip || cells[i, j] == CellStatе.DestroyedShip)
                {
                    hitShipList.Add(new Point(i, j));
                    countHit++;
                }
                len++;
                cells[i + 1, j] = CellStatе.Water;
                if (i + 2 < 10 && (cells[i + 2, j] == CellStatе.Ship|| cells[i + 2, j] == CellStatе.Ship || cells[i+2, j] == CellStatе.DestroyedShip))
                {
                    if (cells[i+2, j] == CellStatе.WoundedShip)
                    {
                        hitShipList.Add(new Point(i, j));
                        countHit++;
                    }
                    len++;
                    cells[i + 2, j] = CellStatе.Water;
                    if (i + 3 < 10 && (cells[i + 3, j] == CellStatе.Ship|| cells[i + 3, j] == CellStatе.Ship || cells[i+3, j] == CellStatе.DestroyedShip))
                    {
                        if (cells[i+3, j] == CellStatе.WoundedShip)
                        {
                            hitShipList.Add(new Point(i, j));
                            countHit++;
                        }
                        len++;
                        cells[i + 3, j] = CellStatе.Water;
                        if (len - countHit == 0) isDead = true;
                        Ships.ShipList4.Add(new Ship4()
                        {
                            IsHorizontal = true,
                            X = i,
                            Y = j,
                            Size = len,
                            CountAlive = len - countHit,
                            IsDead = isDead
                        });
                    }
                    else
                    {
                        if (len - countHit == 0) isDead = true;
                        Ships.ShipList3.Add(new Ship3()
                        {
                            IsHorizontal = true,
                            X = i,
                            Y = j,
                            Size = len,
                            CountAlive = len - countHit,
                            IsDead = isDead
                        });
                    }
                }
                else
                {
                    if (len - countHit == 0) isDead = true;
                    Ships.ShipList2.Add(new Ship2()
                    {
                        IsHorizontal = true,
                        X = i,
                        Y = j,
                        Size = len,
                        IsDead = isDead,
                        CountAlive = len - countHit
                    });
                }
            }
            else if (j + 1 < 10 && (cells[i, j + 1] == CellStatе.Ship|| cells[i, j + 1] == CellStatе.WoundedShip || cells[i, j+1] == CellStatе.DestroyedShip))
            {
                if (cells[i, j+1] == CellStatе.WoundedShip)
                {
                    hitShipList.Add(new Point(i, j+1));
                    countHit++;
                }
                len++;
                cells[i, j + 1] = CellStatе.Water;
                if (j + 2 < 10 && (cells[i, j + 2] == CellStatе.Ship|| cells[i, j + 2] == CellStatе.WoundedShip || cells[i, j+2] == CellStatе.DestroyedShip))
                {
                    if (cells[i, j + 2] == CellStatе.WoundedShip)
                    {
                        hitShipList.Add(new Point(i, j + 2));
                        countHit++;
                    }
                    len++;
                    cells[i, j + 2] = CellStatе.Water;
                    if (j + 3 < 10 && (cells[i, j + 3] == CellStatе.Ship || cells[i, j + 3] == CellStatе.WoundedShip || cells[i, j+3] == CellStatе.DestroyedShip))
                    {
                        if (cells[i, j + 3] == CellStatе.WoundedShip)
                        {
                            hitShipList.Add(new Point(i, j + 3));
                            countHit++;
                        }
                        len++;
                        cells[i, j + 3] = CellStatе.Water;
                        if (len - countHit == 0) isDead = true;
                        Ships.ShipList4.Add(new Ship4()
                        {
                            IsHorizontal = false,
                            X = i,
                            Y = j,
                            Size = len,
                            CountAlive = len - countHit,
                            IsDead = isDead
                        });
                    }
                    else
                    {
                        if (len - countHit == 0) isDead = true;
                        Ships.ShipList3.Add(new Ship3()
                        {
                            IsHorizontal = false,
                            X = i,
                            Y = j,
                            Size = len,
                            CountAlive = len - countHit,
                            IsDead = isDead
                        });
                    }
                }
                else
                {
                    if (len - countHit == 0) isDead = true;
                    Ships.ShipList2.Add(new Ship2()
                    {
                        IsHorizontal = false,
                        X = i,
                        Y = j,
                        Size = len,
                        CountAlive = len - countHit,
                        IsDead = isDead
                    });
                }
            }
            else
            {
                if (len - countHit == 0) isDead = true;
                Ships.ShipList1.Add(new Ship1()
                {
                    IsHorizontal = true,
                    X = i,
                    Y = j,
                    Size = len,
                    CountAlive = len - countHit,
                    IsDead = isDead
                });
            }
        }

        private void setSellFromLoaded(int x, int y)
        {
            Image img = new Image
            {
                Stretch = Stretch.Fill,
                Source = new BitmapImage(new Uri("/Resources/shipCrushed.png", UriKind.Relative)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache },
                //Opacity = 0
            };
            FieldGrid.Children.Add(img);
            Grid.SetColumn(img, x);
            Grid.SetRow(img, y);
        }

        // bool isTimer = false;
        private void SetShotOnField(int x, int y, Grid grid, Image image)
        {
            // _pw = (PlayWindow)((Grid)Parent).Parent;
            // Grid fg = onMyField ? _pw.MyField.FieldGrid : _pw.EnemyField.FieldGrid;
            grid.Dispatcher.Invoke(() =>
            {
                SetCell(x, y, grid, image);
            });
        }


        private void EndOfGame(bool isPlayerWin)
        {
            if (isPlayerWin)
            {
                MessageBoxResult res = MessageBox.Show(WindowConfig.PlayWindowCon, "Вы выиграли!", "Конец игры", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.No, MessageBoxOptions.None);
                if (!_isOnlineGame)
                    WindowConfig.game = null;
                else
                    WindowConfig.OnlineGame = null;
                WindowConfig.PlayWindowCon.Owner.Show();
                WindowConfig.PlayWindowCon.Close();

            }
            else
            {
                MessageBoxResult res = MessageBox.Show(WindowConfig.PlayWindowCon, "Вы проиграли...", "Конец игры", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.No, MessageBoxOptions.None);

                if (!_isOnlineGame)
                    WindowConfig.game = null;
                else
                    WindowConfig.OnlineGame = null;
                WindowConfig.PlayWindowCon.Owner.Show();
                WindowConfig.PlayWindowCon.Close();

            }
        }
    }
}
