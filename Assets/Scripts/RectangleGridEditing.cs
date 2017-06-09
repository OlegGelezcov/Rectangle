using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RectangleGridEditing : MonoBehaviour {

    [SerializeField]
    private int m_Rows;

    [SerializeField]
    private int m_Columns;

    private CellObject[,] m_Grid;

    private Vector2 m_CellSize = new Vector2(50, 50);
    private Vector2 m_CellPadding = new Vector2(2, 2);

    private Dictionary<int, List<MatrixBorder>> m_Stats = new Dictionary<int, List<MatrixBorder>>();
    private List<int> m_AreaKeys = new List<int>();

    void Start() {
        m_Grid = new CellObject[m_Rows, m_Columns];

        for(int i = 0; i < m_Rows; i++ ) {
            for(int j = 0; j < m_Columns; j++ ) {
                m_Grid[i, j] = new CellObject();
            }
        }
    }

    void OnGUI() {
        DrawGrid(m_Grid, m_CellSize, m_CellPadding);

        if(Event.current.isMouse && Event.current.type == EventType.MouseUp ) {
            int row, column;
            ComputeMouseCellIndices(m_Grid, m_CellSize, m_CellPadding, out row, out column);
            if(row >= 0 && column >= 0) {
                m_Grid[row, column].ToggleColor();
            }
        }

        if(GUI.Button(new Rect(Screen.width - 200, Screen.height - 100, 200, 100), "Find")) {
            m_Stats = CollectStats(FindAllOneRectangles(m_Grid));
            m_AreaKeys = new List<int>(m_Stats.Keys);
            m_AreaKeys.Sort((a, b) => { return b.CompareTo(a); });

            if(m_AreaKeys.Count > 0) {
                ShowRects(m_Stats[m_AreaKeys[0]], 2);
            }
            if(m_AreaKeys.Count > 1) {
                ShowRects(m_Stats[m_AreaKeys[1]], 3);
            }
            if (m_AreaKeys.Count > 2) {
                ShowRects(m_Stats[m_AreaKeys[2]], 4);
            }
        }


        if(m_AreaKeys.Count > 0 ) {
            GUI.Label(new Rect(Screen.width * .5f + 100, 20, 500, 30), string.Format("Area: {0}, Count: {1}", m_AreaKeys[0], m_Stats[m_AreaKeys[0]].Count));
        }
        if (m_AreaKeys.Count > 1) {
            GUI.Label(new Rect(Screen.width * .5f + 100, 60, 500, 30), string.Format("Area: {0}, Count: {1}", m_AreaKeys[1], m_Stats[m_AreaKeys[1]].Count));
        }
        if (m_AreaKeys.Count > 2) {
            GUI.Label(new Rect(Screen.width * .5f + 100, 100, 500, 30), string.Format("Area: {0}, Count: {1}", m_AreaKeys[2], m_Stats[m_AreaKeys[2]].Count));
        }
    }

    private void ShowRects(List<MatrixBorder> borders, byte val) {
        foreach(var border in borders) {
            border.ShowOnGrid(m_Grid, val);
        }
    }

    private void DrawGrid(CellObject[,] grid, Vector2 size, Vector2 cellPadding) {

        int rows = grid.GetUpperBound(0) + 1;
        int columns = grid.GetUpperBound(1) + 1;

        Vector2 curPosition = Vector2.zero;
        for(int i = 0; i < rows; i++ ) {
            for(int j = 0; j < columns; j++ ) {
                grid[i, j].Draw(curPosition, size);
                if(j == columns - 1) {
                    curPosition.x = 0;
                    curPosition.y += size.y + cellPadding.y;
                } else {
                    curPosition.x += size.x + cellPadding.x;
                }
            }
        }
    }

    private void ComputeMouseCellIndices(CellObject[,] grid, Vector2 size, Vector2 padding, out int row, out int column) {

        int rows = grid.GetUpperBound(0) + 1;
        int columns = grid.GetUpperBound(1) + 1;

        row = column = -1;

        float x = 0f;
        int counter = 0;
        Vector2 mousePosition = Event.current.mousePosition;

        float xSizeWithPad = size.x + padding.x;
        float ySizeWithPad = size.y + padding.y;

        for(int i = 0; i < rows; i++ ) {
            if(i * xSizeWithPad <= mousePosition.x &&  mousePosition.x < (i + 1) * xSizeWithPad) {
                column = i;
                break;
            }
        }

        for(int j = 0; j < columns; j++ ) {
            if( j * ySizeWithPad <= mousePosition.y && mousePosition.y < (j + 1) * ySizeWithPad ) {
                row = j;
                break;
            }
        }
    }

    //Самый очевидный способ. Брут форс. Просто перебираем все возможные прямоугольники, исключаем пересечения, сортируем и выдаем результат
    //Время выполнения O(N**4)
    private List<MatrixBorder> FindAllOneRectangles(CellObject[,] grid) {

        List<MatrixBorder> borders = new List<MatrixBorder>();

        int rows = grid.GetUpperBound(0) + 1;
        int columns = grid.GetUpperBound(1) + 1;

        for(int rowLower = 0; rowLower < rows; rowLower++ ) {
            for(int colLower = 0; colLower < columns; colLower++ ) {
                for(int rowUpper = rowLower; rowUpper < rows; rowUpper++ ) {
                    for(int colUpper = colLower; colUpper < columns; colUpper++ ) {
                        MatrixBorder border = new MatrixBorder { minRow = rowLower, maxRow = rowUpper, minColumn = colLower, maxColumn = colUpper };
                        if(border.IsAllNonZeros(grid) && border.area > 0) {
                            borders.Add(border);
                        }
                    }
                }
            }
        }

        borders.Sort((a, b) => {
            return b.area.CompareTo(a.area);
        });

        List<MatrixBorder> result = new List<MatrixBorder>();
        if(borders.Count > 0 ) {
            result.Add(borders[0]);
        }

        for(int i = 1; i < borders.Count; i++ ) {
            if(!IsIntersects(result, borders[i])) {
                result.Add(borders[i]);
            }
        }

        return result;
    }

    private bool IsIntersects(List<MatrixBorder> borders, MatrixBorder testBorder) {
        foreach(var bord in borders ) {
            if(bord.Intersect(testBorder)) {
                return true;
            }
        }
        return false;
    }

    private Dictionary<int, List<MatrixBorder>> CollectStats(List<MatrixBorder> borders) {
        Dictionary<int, List<MatrixBorder>> stats = new Dictionary<int, List<MatrixBorder>>();
        foreach(var border in borders ) {
            if(stats.ContainsKey(border.area)) {
                stats[border.area].Add(border);
            } else {
                stats.Add(border.area, new List<MatrixBorder> { border });
            }
        }
        return stats;
    }

    #region Unsuccessfull algotihm
    /*
        Алгоритм, основанный на поиске прямоугольника с максимальной площадью в гистограмме
        Пока не получается
        Хотел адаптировать этот алгоритм, но на это надо больше времени
        http://www.geeksforgeeks.org/maximum-size-rectangle-binary-sub-matrix-1s/
    */

    /*
    private byte Min(byte a, byte b, byte c) {
        byte m = a;
        if(m > b ) {
            m = b;
        }
        if(m > c ) {
            m = c;
        }
        return m;
    }

    private int MaxHist(int[] row) {
        Stack<int> result = new Stack<int>();
        int topVal = 0, maxArea = 0, area = 0;
        int i = 0;

        while(i < row.Length ) {
            if ((result.Count == 0) || row[result.Peek()] <= row[i]) {
                result.Push(i++);
            } else {
                topVal = row[result.Peek()];
                result.Pop();
                area = topVal * i;
                if(result.Count != 0 ) {
                    area = topVal * (i - result.Peek() - 1);
                }
                maxArea = Mathf.Max(area, maxArea);
            }
        }
        while(result.Count != 0 ) {
            topVal = row[result.Peek()];
            result.Pop();
            area = topVal * i;
            if(result.Count != 0 ) {
                area = topVal * (i - result.Peek() - 1);
            }
            maxArea = Mathf.Max(area, maxArea);
        }
        return maxArea;
    }


    private int[,] Transposed(int[,] matr) {
        int rows = matr.GetUpperBound(0) + 1;
        int columns = matr.GetUpperBound(1) + 1;
        int[,] result = new int[columns, rows];
        for(int i = 0; i < rows; i++ ) {
            for(int j = 0; j < columns; j++ ) {
                result[j, i] = matr[i, j];
            }
        }
        return result;
    }

    private int[,] Reflected(int[,] matr) {
        int rows = matr.GetUpperBound(0) + 1;
        int columns = matr.GetUpperBound(1) + 1;
        int[,] result = new int[rows, columns];
        for(int i = 0; i < rows; i++ ) {
            for(int j = 0; j < columns; j++ ) {
                result[i, j] = matr[rows - i - 1, j];
            }
        }
        return result;
    }

    private int[,] SubMatrix(int[,] matr, MatrixBorder border) {
        int[,] result = new int[border.rows, border.columns];
        if (border.IsValid(matr)) {
            for(int i = 0; i < border.rows; i++ ) {
                for(int j = 0; j < border.columns; j++ ) {
                    result[i, j] = matr[border.minRow + i, border.minColumn + j];
                } 
            }
        }
        return result;
    }

    private int[,] MatrixCopy(int[,] matr) {
        return SubMatrix(matr, new MatrixBorder { minRow = 0, minColumn = 0, maxRow = matr.GetUpperBound(0), maxColumn = matr.GetUpperBound(1) });
    }

    private int[] GetSubRow(int[,] matr, int cmin, int cmax, int r) {
        int[] result = new int[cmax - cmin + 1];
        for(int i = cmin; i <= cmax; i++ ) {
            result[i] = matr[r, i];
        }
        return result;
    }

    private int[] GetSubColumn(int[,] matr, int rmin, int rmax, int c) {
        int[] result = new int[rmax - rmin + 1];
        for(int i = rmin; i <= rmax; i++  ) {
            result[i] = matr[i, c];
        }
        return result;
    }


    private int MaxRectangle(int[,] matr) {
        int rows = matr.GetUpperBound(0) + 1;
        int columns = matr.GetUpperBound(1) + 1;

        int result = MaxHist(GetSubRow(matr, 0, columns-1, 0));
        int targetRow = 0;

        for(int i = 1; i < rows; i++ ) {
            for(int j = 0; j < columns; j++ ) {
                if(matr[i, j] != 0 ) {
                    matr[i, j] += matr[i - 1, j];
                }
            }
            int test = MaxHist(GetSubRow(matr, 0, columns - 1, i));
            if(test > result ) {
                result = test;
                targetRow = i;
            }
        }
        return targetRow;
    }


    private MatrixBorder MaxBorder(int[,] matr, int startRow = 0, int startColumn = 0) {
        int rows = matr.GetUpperBound(0) + 1;
        int columns = matr.GetUpperBound(1) + 1;

        var h1Matr = MatrixCopy(matr);
        int rowMax = MaxRectangle(h1Matr);

        var h2Matr = Reflected(matr);
        int rowMin = rows - 1 - MaxRectangle(h2Matr);

        var c1Matr = Transposed(matr);
        var colMax = MaxRectangle(c1Matr);

        var c2Matr = Reflected(Transposed(matr));
        var colMin = columns - 1 - MaxRectangle(c2Matr);

        return new MatrixBorder { minRow = rowMin + startRow, maxRow = rowMax + startRow, minColumn = colMin + startColumn, maxColumn = colMax + startColumn};
    }

    private void ConstructBorders(int[,] matr, List<MatrixBorder> borders, MatrixBorder parentBorder) {
        int rows = matr.GetUpperBound(0) + 1;
        int columns = matr.GetUpperBound(1) + 1;

        var result = MaxBorder(matr, parentBorder.minRow, parentBorder.maxRow);

        if (result.IsValid(matr)) {
            borders.Add(result);

            List<MatrixBorder> childBorders = new List<MatrixBorder>();
            if (result.minRow > 0) {
                //MatrixBorder bord = new MatrixBorder
            }
        }
    }*/

    #endregion

    void Update() {
        if(Input.GetKeyUp(KeyCode.A )) {
            foreach(var bord in FindAllOneRectangles(m_Grid)) {
                print(bord.ToString());
            }
        }
    }
}

public struct MatrixBorder {
    public int minColumn;
    public int maxColumn;
    public int minRow;
    public int maxRow;

    
    public int rows {
        get {
            return maxRow - minRow + 1;
        }
    }

    public int columns {
        get {
            return maxColumn - minColumn + 1;
        }
    }

    public int area {
        get {
            return rows * columns;
        }
    }

    public void ShowOnGrid(CellObject[,] grid, byte val) {
        for(int i = minRow; i <= maxRow; i++ ) {
            for(int j = minColumn; j <= maxColumn; j++ ) {
                grid[i, j].SetValue(val);
            }
        }
    }

    public bool Intersect(MatrixBorder other ) {
        return (minColumn <= other.maxColumn && maxColumn >= other.minColumn && minRow <= other.maxRow && maxRow >= other.minRow) || IsEqual(other);
        //return (maxColumn < other.minColumn || other.maxColumn < minColumn || maxRow < other.minRow || other.maxRow < minRow);
    }

    private bool IsEqual(MatrixBorder other) {
        return (minRow == other.minRow) && (maxRow == other.maxRow) && (minColumn == other.minColumn) && (maxColumn == other.maxColumn);
    }

    public bool IsAllNonZeros(CellObject[,] grid) {
        for(int i = minRow; i <= maxRow; i++ ) {
            for(int j = minColumn; j <= maxColumn; j++ ) {
                if(grid[i, j].value == 0 ) {
                    return false;
                }
            }
        }
        return true;
    }

    public bool IsValid(int[,] matr) {
        int rows = matr.GetUpperBound(0) + 1;
        int columns = matr.GetUpperBound(1) + 1;
        return IsIndexValid(minColumn, columns) && IsIndexValid(maxColumn, columns) &&
            IsIndexValid(minRow, rows) && IsIndexValid(maxRow, rows) && (minRow <= maxRow) && (minColumn <= maxColumn);
    }

    private bool IsIndexValid(int index, int length) {
        return 0 <= index && index < length;
    }

    public override string ToString() {
        return string.Format("({0},{1})  ({2},{3})", minRow, minColumn, maxRow, maxColumn);
    }
}

public class CellObject {
    private Texture2D m_Tex;
    private byte m_Value;

    public byte value {
        get {
            return m_Value;
        }
    }

    public CellObject() {
        m_Tex = new Texture2D(1, 1);
        m_Tex.wrapMode = TextureWrapMode.Repeat;
        m_Tex.SetPixel(0, 0, Color.black);
        m_Tex.Apply();
        m_Value = 0;
    }
    public void SetValue(byte val ) {
        m_Value = val;

        switch(val) {
            case 0: {
                    m_Tex.SetPixel(0, 0, Color.black);
                }
                break;
            case 1: {
                    m_Tex.SetPixel(0, 0, Color.white);
                }
                break;
            case 2: {
                    m_Tex.SetPixel(0, 0, Color.yellow);
                }
                break;
            case 3: {
                    m_Tex.SetPixel(0, 0, Color.green);
                }
                break;
            case 4: {
                    m_Tex.SetPixel(0, 0, Color.magenta);
                }
                break;
            default: {
                    m_Tex.SetPixel(0, 0, Color.black);
                }
                break;
        }

        m_Tex.Apply();

    }



    public void ToggleColor( ) {
        SetValue((byte)((m_Value != 0) ? 0 : 1));
    }

    public void Draw(Vector2 position, Vector2 size) {
        GUI.DrawTexture(new Rect(position, size), m_Tex);
    }
}
