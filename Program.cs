using Excercise;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Xml.Linq;

namespace GridmapSearch
{
    internal class Program
    {

        static bool editMode = false;

        static void Main(string[] args)
        {
            Console.Clear();
            //맵 크기 결정
            int[] input = Array.ConvertAll(Console.ReadLine().Split(), int.Parse);
            Vector2Int[] vec = new Vector2Int[3];

            for (int i = 0; i < 2; i++)
            {
                var currentVecInt = Array.ConvertAll(Console.ReadLine().Split(), int.Parse);
                vec[i] = new Vector2Int(currentVecInt[0], currentVecInt[1]);
            }

            //벽 생성 커서 위치 초기화
            vec[2] = Vector2Int.zero;

            //벽 지도 초기화
            bool[,] collisionMap = new bool[input[1], input[0]];  

            Console.Clear();

            var inputString = "";
            var selectIndex = 1;
            var radiusCost = 0;
            ConsoleKey s = ConsoleKey.Spacebar;

            while (s != ConsoleKey.Escape)
            {
                inputString = $"Start -> [{vec[0].x},{vec[0].y}] | End -> [{vec[1].x},{vec[1].y}]";
                Console.WriteLine(inputString);

                //길찾기
                PathFind(input[0], input[1], vec[0], vec[1], collisionMap, out var points, radiusCost);

                TileRender(input[0], input[1], vec[0], vec[1], vec[2], collisionMap, points);

                s = Console.ReadKey().Key;
                
                switch (s)
                {
                    case ConsoleKey.UpArrow:
                        vec[selectIndex] += new Vector2Int(0, 1);
                        break;
                    case ConsoleKey.DownArrow:
                        vec[selectIndex] += new Vector2Int(0, -1);
                        break;
                    case ConsoleKey.RightArrow:
                        vec[selectIndex] += new Vector2Int(1, 0);
                        break;
                    case ConsoleKey.LeftArrow:
                        vec[selectIndex] += new Vector2Int(-1, 0);
                        break;
                    case ConsoleKey.Oem3:
                        Console.Write("\r ");
                        Console.WriteLine();
                        Console.WriteLine("Start / End 선택 / Wall 선택 , 0 : start, 1 : end , 2 : wall");
                        string currentString = Console.ReadLine();
                        if (int.TryParse(currentString, out int outNum))
                        {
                            selectIndex = Math.Clamp(outNum, 0, vec.Length - 1);
                        }
                        else if(currentString == "!setCost")
                        {
                            radiusCost = int.Parse(Console.ReadLine());
                        }
                        break;
                }

                editMode = selectIndex == 2 ? true : false;

                if (selectIndex != 2)
                    vec[2] = vec[selectIndex];

                if (vec[selectIndex].x < 0 || vec[selectIndex].x > input[0] - 1)
                    vec[selectIndex].x = vec[selectIndex].x < 0 ? 0 : input[0] - 1;
                if (vec[selectIndex].y < 0 || vec[selectIndex].y > input[1] - 1)
                    vec[selectIndex].y = vec[selectIndex].y < 0 ? 0 : input[1] - 1;

                if (editMode && s == ConsoleKey.Enter)
                    collisionMap[vec[2].y, vec[2].x] = !collisionMap[vec[2].y, vec[2].x];

                Console.Clear();
            }
        }

        static void TileRender(int x, int y, Vector2Int start, Vector2Int end, Vector2Int cursor, bool[,] collisionMap, List<Vector2Int> pathPoints)
        {
            bool[,] pathPointMap = new bool[x, y];

            foreach(var path in pathPoints)
            {
                pathPointMap[path.x, path.y] = true;
                //Console.WriteLine($"{path.x} + {path.y}");
            }

            for (int i = y - 1; i > -1; i--)
            {
                for (int j = 0; j < x; j++)
                {
                    if (editMode && j == cursor.x && i == cursor.y)
                    {
                        Console.Write("* ");
                    }
                    else if (j == start.x && i == start.y)
                    {
                        Console.Write("0 ");
                    }
                    else if (j == end.x && i == end.y)
                    {
                        Console.Write("1 ");
                    }
                    else if (pathPointMap[j, i])
                    {
                        Console.Write("# ");
                    }
                    else if (collisionMap[i, j])
                    {
                        Console.Write("@ ");
                    }
                    else
                    {
                        Console.Write(". ");
                    }
                    if (j == x - 1)
                    {
                        Console.WriteLine();
                    }
                        
                }
            }
        }


        static void PathFind(int mapX, int mapY, Vector2Int start, Vector2Int end, bool[,] collisionMap, out List<Vector2Int> points, int radiusCost = 0)
        {
            points = new List<Vector2Int>();
            Vector2Int[] deltaPos = { Vector2Int.up, Vector2Int.left, Vector2Int.down, Vector2Int.right };
            bool[,] closed = new bool[mapY, mapX];
            int[,] open = new int[mapY, mapX];
            for (int i = 0; i < mapX; i++)
                for (int j = 0; j < mapY; j++)
                    open[j, i] = Int32.MaxValue;
            Vector2Int[,] parent = new Vector2Int[mapY, mapX];
            Excercise.PriorityQueue<PathNode> pq = new PriorityQueue<PathNode>();

            open[start.y, start.x] = Math.Abs(mapY - start.y) + Math.Abs(mapX - start.x);
            pq.Push(new PathNode { G = 0, H = Math.Abs(mapY - start.y) + Math.Abs(mapX - start.x), pos = start });
            parent[start.y, start.x] = new Vector2Int(start.x, start.y);

            while (pq.Count > 0)
            {
                PathNode node = pq.Pop();

                if (closed[node.pos.y, node.pos.x])
                    continue;

                closed[node.pos.y, node.pos.x] = true;

                if (node.pos == end)
                    break;

                for (int i = 0; i < deltaPos.Length; i++)
                {
                    var next = node.pos + deltaPos[i];
                    //Console.WriteLine("!");

                    if (next.x < 0 || next.x >= mapX || next.y < 0 || next.y >= mapY)
                        continue;
                    //Console.WriteLine("?");
                    if (closed[next.y, next.x])
                        continue;
                    if (collisionMap[next.y, next.x])
                        continue;

                    int g = node.G + 1;
                    int h = Math.Abs(mapY - next.y) + Math.Abs(mapX - next.x);

                    //Console.WriteLine($"{g} + {h}");

                    if (open[next.y, next.x] < g + h)
                        continue;

                    open[next.y, next.x] = g + h;
                    pq.Push(new PathNode { G = g, H = h, pos = next });
                    parent[next.y, next.x] = new Vector2Int(node.pos.x, node.pos.y);
                }
            }

            CalcPathFormParent(parent, end.x, end.y, points, radiusCost);
        }

        private static void CalcPathFormParent(Vector2Int[,] parent,int destX,int destY, List<Vector2Int> points, int cost = 0)
        {
            Vector2Int currentPos = new Vector2Int(destX, destY);

            while (parent[currentPos.y,currentPos.x].y != currentPos.y || parent[currentPos.y, currentPos.x].x != currentPos.x)
            {
                //반경 카운트, 카운트를 소모하여 그 이후 부터 뒤로 추적
                if (cost == 0)
                    points.Add(new Vector2Int(currentPos.x, currentPos.y));
                else
                    cost--;
                var newPos = parent[currentPos.y, currentPos.x];
                currentPos.x = newPos.x;
                currentPos.y = newPos.y;
            }
            points.Add(new Vector2Int(currentPos.x, currentPos.y));
            points.Reverse();
        }
    }

    public struct PathNode : IComparable<PathNode>
    {
        public int G;
        public int H;
        public int F => G + H;
        public Vector2Int pos;

        public int CompareTo(PathNode other)
        {
            if (F == other.F)
                return 0;
            return F > other.F ? 1 : -1; 
        }
    }

    public struct Vector2Int
    {
        public int x, y;

        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static bool operator ==(Vector2Int a, Vector2Int b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(Vector2Int a, Vector2Int b)
        {
            return !(a == b);
        }

        public static Vector2Int operator +(Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x + b.x, a.y + b.y);
        }

        public static Vector2Int operator -(Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x - b.x, a.y - b.y);
        }

        public static Vector2Int operator *(Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x * b.x, a.y * b.y);
        }

        public static Vector2Int operator /(Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x / b.x, a.y / b.y);
        }

        public override bool Equals(object other)
        {
            return base.Equals(other);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public static Vector2Int up => new Vector2Int(0, 1);
        public static Vector2Int down => new Vector2Int(0, -1);
        public static Vector2Int right => new Vector2Int(1, 0);
        public static Vector2Int left => new Vector2Int(-1, 0);
        public static Vector2Int zero => new Vector2Int(0, 0);
    }
}