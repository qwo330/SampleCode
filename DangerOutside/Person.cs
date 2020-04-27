using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Person : MonoBehaviour
{
    #region inspector
    [Header("Inspector")]
    [SerializeField]
    citizen_Type kind;

    [SerializeField]
    float MoveSpeed = 1f;

    public float CleanTime = 30f;

    [SerializeField]
    float IdleTime = 3f;

    [SerializeField]
    int MoveRange = 4;

    [Header("Probability")]
    public int WantIn;
    public int WantOut;

    public int WantHome;
    public int WantBath;
    public int WantChurch;
    public int WantSing;
    public int WantSchool;
    public int WantOffice;
    public int WantNoWhere;

    public int WantWhite;
    public int WantRed;
    public int WantBlue;
    public int WantNothing;
    #endregion

    [Header("Reference")]
    [SerializeField]
    SpeechBubble Bubble;

    [SerializeField]
    Animator animator;

    [HideInInspector]
    public bool cleanerOn = false;

    [HideInInspector]
    public Vector2 pos = Vector2.one * -1; // 현재 위치한 타일의 좌표값

    public Apartment Home { get; private set; }
    Building targetBuilding;

    CitizenColor color = CitizenColor.White;
    Ballone ballone = Ballone.Idle;
    NodeType nodeType;

    TileController tileController;
    AStar astar;
    Stack<Node> Path;
    Node destination;

    public bool ForceMove; // 터치 반응 없이 강제로 도착지까지 경로 설정
    bool isMoveTo;
    Coroutine co_FSM;

    #region 오브젝트 생성 & 활성
    public void CreateSetting(TileController tileController)
    {
        this.tileController = tileController;
        astar = new AStar(tileController);
        InitCitizen();
    }
    
    public void ResetPos(EditorTile tile)
    {
        Vector3 tilepos = tile.transform.position;
        tilepos.z += 100;
        transform.position = tilepos;
    }

    public void InitCitizen()
    {
        ChangeColor(CitizenColor.White);
        ChangePattern();
    }

    public void StartFSM()
    {
        ChangeColor(color);
        ChangePattern();
        co_FSM = StartCoroutine(FSM());
    }

    void OnDisable()
    {
        if (co_FSM != null)
            StopCoroutine(co_FSM);

        Bubble.HideSpeechBubble();
    }
    #endregion

    #region AI 패턴
    IEnumerator FSM()
    {
        while (true)
        {
            switch (ballone)
            {
                case Ballone.Move:
                    if (Path != null && Path.Count != 0)
                        yield return MoveTo();
                    else
                        ChangePattern();
                    break;
                case Ballone.Idle:
                    ChangeAnimatorDir(Vector2.zero);
                    ChangePattern();
                    yield return new WaitForSeconds(IdleTime);
                    break;
            }

            yield return null;
        }
    }

    IEnumerator MoveTo()
    {
        yield return null;
        isMoveTo = true;
        Vector2 target = Path.Pop().Position;

        EditorTile tile = tileController.tiles[(int)target.x, (int)target.y];
        ChangeAnimatorDir(target);
        pos = tile.pos;
        PreInteractTile(tile);

        while (Approximately(transform.position, tile.transform.position) == false)
        {
            float step = MoveSpeed * Time.deltaTime;

            Vector3 tilepos = tile.transform.position;
            tilepos.z += 100;
            transform.position = Vector3.MoveTowards(transform.position, tilepos, step);

            yield return null;
        }

        PostInteractTile(tile);

        // 도착 체크 및 처리
        if (Path.Count <= 0)
        {
            // 건물
            if (targetBuilding != null && NodeType.Home <= nodeType && nodeType <= NodeType.Company)
            {
                if (isMoveTo) // 터치로 패턴 변경 시 타이밍 에러 방지
                {
                    targetBuilding.Come_In(gameObject);
                    ForceMove = false;
                    destination = null;
                }
            }
            else // 타일
            {
                destination = null;
                ChangePattern();
            }
        }

        isMoveTo = false;
    }

    public void ChangePattern()
    {
        int rand = Random.Range(0, 2);
        Ballone pattern;

        if (rand == 0)
            pattern = Ballone.Move;
        else
            pattern = Ballone.Idle;

        ChangePattern(pattern);
    }

    public void ChangePattern(Ballone newBallone, bool findPath = true)
    {
        isMoveTo = false;
        if (Path != null && findPath)
            Path.Clear();

        ballone = newBallone;
        if (newBallone == Ballone.Move && findPath)
            FindPath();
    }
    #endregion

    #region 경로 탐색
    public void FindPath()
    {
        int tx = (int)pos.x;
        int ty = (int)pos.y;
        var area = GetArea(tx, ty);

        RandomBuilding();
        var newArea = area.FindAll(n => n != null && n.Type == nodeType);

        if (nodeType != NodeType.Nothing)
        {
            targetBuilding = BuildingManager.Instance.Get_Building(nodeType, Home);
        }
        else if (nodeType == NodeType.Nothing || newArea.Count == 0)
        {
            RandomTile();
            newArea = area.FindAll(n => n != null && n.Type == nodeType);
        }

        if (nodeType == NodeType.School && newArea.Count != 0)
            Debug.Log("### Find Path School");

        if (newArea.Count == 0)
        {
            ChangePattern();
        }
        else
        {

            Node startNode = tileController.Map[tx, ty];
            destination = newArea.RandomPick(startNode);
            //Debug.LogFormat("목적지 : {0},{1}", destination.X, destination.Y);

            Path = astar.FindPath(startNode, destination);
            if (Path != null)
                ShowSpeechBubble(nodeType);

            // LogAllPath();
        }
    }

    /// <summary>
    /// 행동 반경에 관계없이 해당 건물로 이동
    /// </summary>
    public void GoBulding(Building building)
    {
        ForceMove = true;
        ShowSpeechBubble(building.kind);

        if (Object_Build_Type.Small <= building.kind && building.kind <= Object_Build_Type.Big)
            nodeType = NodeType.Home;
        else
            nodeType = (NodeType)building.kind - 2;

        targetBuilding = building;
        int buildingX = (int)building.pos.x;
        int buildingY = (int)building.pos.y;

        int tx = (int)pos.x;
        int ty = (int)pos.y;

        Node startNode = tileController.Map[tx, ty];
        destination = tileController.Map[buildingX, buildingY];
        Path = astar.FindPath(startNode, destination);

        ChangePattern(Ballone.Move, false);
    }

    List<Node> GetArea(int tx, int ty)
    {
        int halfRange = (int)(MoveRange * 0.5f);

        int minX = Mathf.Max(0, tx - halfRange);
        int maxX = Mathf.Min(tx + halfRange, TileController.x_max_value);
        int minY = Mathf.Max(0, ty - halfRange);
        int maxY = Mathf.Min(ty + halfRange, TileController.y_max_value);

        List<Node> area = new List<Node>();
        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                Node node = tileController.Map[x, y];
                if (tileController.banTileList.Contains(node) == false)
                    area.Add(node);
            }
        }

        return area;
    }

    public void RandomBuilding()
    {
        int randSum = WantHome + WantBath + WantChurch + WantSing + WantSchool + WantOffice + WantNoWhere;
        int rand = Random.Range(0, randSum);

        int next = 0;
        foreach (int p in GetBuildingProbability())
        {
            next += p;
            if (rand < next) // Pick
                break;
        }
    }

    public void RandomTile()
    {
        int randSum = WantWhite + WantRed + WantBlue + WantNothing;
        int rand = Random.Range(0, randSum);

        int next = 0;
        foreach (int p in GetTileProbability())
        {
            next += p;
            if (rand < next) // Pick
                break;
        }
    }

    IEnumerable GetBuildingProbability()
    {
        nodeType = NodeType.Home;
        yield return WantHome;

        nodeType = NodeType.BathHouse;
        yield return WantBath;

        nodeType = NodeType.Church;
        yield return WantChurch;

        nodeType = NodeType.Karaoke;
        yield return WantSing;

        nodeType = NodeType.School;
        yield return WantSchool;

        nodeType = NodeType.Company;
        yield return WantOffice;

        nodeType = NodeType.Nothing;
        yield return WantNoWhere;
    }

    IEnumerable GetTileProbability()
    {
        nodeType = NodeType.None;
        yield return WantNothing;

        nodeType = NodeType.White;
        yield return WantWhite;

        nodeType = NodeType.Red;
        yield return WantRed;

        nodeType = NodeType.Blue;
        yield return WantBlue;
    }
    #endregion

    bool Approximately(Vector2 current, Vector2 target)
    {
        //bool same_x = Mathf.Approximately(current.x, target.x);
        //bool same_y = Mathf.Approximately(current.y, target.y);
        //return same_x && same_y;

        return Vector2.Distance(current, target) < 0.001f;
    }

    // 이동 방향에 따른 캐릭터 애니메이션 호출
    public void ChangeAnimatorDir(Vector2 target)
    {
        if (target == Vector2.zero) // idle, Map[0,0]은 사용하지 않으므로 문제 없을것으로 예상됨
        {
            animator.SetBool("stop", true);
            animator.SetBool("back", false);
            animator.SetBool("side", false);
        }
        else if (Mathf.Approximately(pos.x, target.x))
        {
            if (target.y - pos.y > 0) // up // back
            {
                animator.SetBool("stop", false);
                animator.SetBool("back", true);
                animator.SetBool("side", false);
            }
            else if (target.y - pos.y < 0) // down // side
            {
                animator.SetBool("stop", false);
                animator.SetBool("back", false);
                animator.SetBool("side", true);
            }
        }
        else
        {
            if (target.x - pos.x > 0) // right // back
            {
                animator.SetBool("stop", false);
                animator.SetBool("back", true);
                animator.SetBool("side", false);
            }
            else if (target.x - pos.x < 0) // left // side
            {
                animator.SetBool("stop", false);
                animator.SetBool("back", false);
                animator.SetBool("side", true);
            }
        }
    }

    public void ChangeColor(CitizenColor newColor)
    {
        color = newColor;
        ChangeColorAnimator(newColor);
    }

    void ChangeColorAnimator(CitizenColor color)
    {
        switch (color)
        {
            case CitizenColor.White:
                animator.SetBool("white", true);
                animator.SetBool("red", false);
                animator.SetBool("blue", false);
                break;
            case CitizenColor.Red:
                animator.SetBool("white", false);
                animator.SetBool("red", true);
                animator.SetBool("blue", false);
                break;
            case CitizenColor.Blue:
                animator.SetBool("white", false);
                animator.SetBool("red", false);
                animator.SetBool("blue", true);
                break;
        }
    }

    public CitizenColor GetCitizenColor()
    {
        return color;
    }

    public citizen_Type GetCitizenKind()
    {
        return kind;
    }

    public void SetCitizenHome(Apartment home)
    {
        Home = home;
        //astar.SetHome(Home);
    }

    void ShowSpeechBubble(NodeType nodeType)
    {
        EmoticonType emoticon;
        if (NodeType.Home <= nodeType && nodeType <= NodeType.Company)
            emoticon = (EmoticonType)nodeType;
        else if (NodeType.White <= nodeType && nodeType <= NodeType.Blue)
            emoticon = EmoticonType.Walk;
        else
            emoticon = EmoticonType.Question;

        Bubble.ShowSpeechBubble(emoticon, color == CitizenColor.Red);
    }

    void ShowSpeechBubble(Object_Build_Type kind)
    {
        if (kind > Object_Build_Type.Company)
            return;

        EmoticonType emoticon = 0;
        if (Object_Build_Type.Small <= kind && kind <= Object_Build_Type.Big)
            emoticon = EmoticonType.House;
        else
            emoticon = (EmoticonType)kind - 2;

        Bubble.ShowSpeechBubble(emoticon, color == CitizenColor.Red);
    }

    void PreInteractTile(EditorTile tile)
    {
        if (color == CitizenColor.Red)
        {
            if (tile.tile_Type == Tile_Type.White)
                tile.TileChange(Tile_Type.Red);
            else if (tile.tile_Type == Tile_Type.Blue)
                tile.TileChange(Tile_Type.White);
        }
    }

    void PostInteractTile(EditorTile tile)
    {
        if (tile.tile_Type == Tile_Type.Red)
        {
            if (color == CitizenColor.White)
                ChangeColor(CitizenColor.Red);
            else if (color == CitizenColor.Blue)
                ChangeColor(CitizenColor.White);
        }
        else if (tile.tile_Type == Tile_Type.Blue)
        {
            if (color == CitizenColor.White)
                ChangeColor(CitizenColor.Blue);
        }
    }

    public void OnClickPerson()
    {
        if (ForceMove)
            return;

        Debug.Log("### Click Citizen");
        ChangePattern();
    }

#if UNITY_EDITOR
    Vector3 currentTile, nextTile;
    void OnDrawGizmos()
    {
        if (Path == null || Path.Count == 0)
            return;

        Gizmos.color = Color.black;

        var player = tileController.tiles[(int)pos.x, (int)pos.y];
        nextTile = player.transform.position;

        foreach (var node in Path)
        {
            var tile = tileController.tiles[node.X, node.Y];
            currentTile = nextTile;
            nextTile = tile.transform.position;
            Gizmos.DrawLine(currentTile, nextTile);
        }
    }

    void LogAllPath()
    {
        string debugPath = "";
        foreach (var item in Path)
        {
            debugPath += string.Format("{0},{1}  ", item.X, item.Y);
        }
        Debug.Log(debugPath);
    }
#endif
}