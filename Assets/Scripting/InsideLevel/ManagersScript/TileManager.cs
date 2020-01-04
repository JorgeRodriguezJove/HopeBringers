﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour

{
    #region VARIABLES

    [SerializeField]
    private LayerMask obstacleMask;

    [SerializeField]
    private LayerMask noTileHereMask;

    [SerializeField]
    private LayerMask startignTileMask;


    //Tamaño del área dónde va a haber tiles
    public Vector3 gridWorldSize;
    //Radio de los tiles
    public float nodeRadius;
    //Array de 3 dimensiones con los objetos que van a tener el script de node.
    GameObject[,,] gridObject;
    //Array de 3 dimensiones con los scripts. IMPORTANTE ESTE SOLO SE USA AL COMIENZO LUEGO SE USA EL DE 2 DIMENSIONES
    IndividualTiles[,,] grid3DNode;

    //Array de 2 Dimensiones que se usa para el pathfinding.
    IndividualTiles[,] grid2DNode;

    //Prefab del tile
    [SerializeField]
    GameObject tilePref;

    float nodeDiameter;
    int gridSizeX, gridSizeZ;

    int gridSizeY;

    //Colores tiles
    [SerializeField]
    private Material availableForMovementColor;
    [SerializeField]
    private Material currentTileHoverMovementColor;
    [SerializeField]
    private Material attackColor;
    [SerializeField]
    private Material chargingAttackColor;
    [SerializeField]
    private Material actionRangeColor;

    [Header("FUNCIÓN CREAR PATH")]

    //Diccionario con distancia a nodos
    Dictionary<IndividualTiles, float> dist = new Dictionary<IndividualTiles, float>();
    //Diccionario con nodos que forman el camino para llegar al objetivo.
    Dictionary<IndividualTiles, IndividualTiles> prev = new Dictionary<IndividualTiles, IndividualTiles>();
    //Lista con los nodos que todavía no han sido comprobados al buscar el camino.
    [SerializeField]
    public List<IndividualTiles> unvisited = new List<IndividualTiles>();

    //Punto de origen (Nodo en el que está el personaje).
    IndividualTiles source;

    //Casilla objetivo a la que queremos llegar.
    IndividualTiles target;

    //Current tile que se está comprobando para hacer el path (antes de invertir el path).
    IndividualTiles curr;

    [Header("PATHFINDING")]

    //Variable que se usa para almacenar el resultado del pathfinding y enviarlo.
    float tempCurrentPathCost;

    //Personaje actualmente seleccionado
    private UnitBase selectedCharacter;

    //Lista de tiles que forman el path desde un tile hasta otro. Al igual que temCurrentPathCost se resetea cada vez que se llama a la función CalculatePathForMovement
    [HideInInspector]
    public List<IndividualTiles> currentPath = new List<IndividualTiles>();

    //Almaceno el tile wue estoy comprobando aora mismo para no acceder todo el rato desde el selected character
    private IndividualTiles currentTileCheckingForMovement;

    //Tiles que actualmente están dispoibles para el movimiento de la unidad seleccionada.
    //La pongo pública para que el enemigo pueda acceder.
    public List<IndividualTiles> tilesAvailableForMovement = new List<IndividualTiles>();

    //Lista de tiles sin visitar
    List<IndividualTiles> openList = new List<IndividualTiles>();
    //HasSet de tiles visitados. (En el HashSet no puede haber elementos repetidos aunque no puedes acceder directametne a un elemento con [])
    HashSet<IndividualTiles> closedHasSet = new HashSet<IndividualTiles>();

    [Header("ENEMY_PATHFINDING")]

    ////Enemigo actual que está calculando su pathfinding
    //private EnemyUnit selectedEnemy;

    //Lista de posibles objetivos del enemigo actual
    List<UnitBase> charactersAvailableForAttack = new List<UnitBase>();

    //Variable que se usa para almacenar la distancia con el objetivo
    float tempCurrentObjectiveCost;

    //Es el equivalente a currentTileCheckingForMovement para buscar enemigos en rango para el goblin
    private IndividualTiles currentTileCheckingForUnit;

    //Lista que le paso al goblin con los enemigos en rango
    private List<UnitBase> unitsInRangeWithoutPathfinding = new List<UnitBase>();

    //Las mismas variables que para el CheckAvailableMovement pero para las acciones enemigas
    public List<IndividualTiles> tilesAvailableForEnemyAction = new List<IndividualTiles>();
    private IndividualTiles currentTileCheckingForEnemyAction;

    [Header("REFERENCIAS")]
    [SerializeField]
    LevelManager LM;

    #endregion

    #region INIT

    private void Awake()
    {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeZ = Mathf.RoundToInt(gridWorldSize.z / nodeDiameter);

        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        CreateGrid();
    }

    void CreateGrid()
    {
        //Inicializo el array y la posición en la que se inicia a comprobar el grid
        gridObject = new GameObject[gridSizeX, gridSizeY, gridSizeZ];
        grid3DNode = new IndividualTiles[gridSizeX, gridSizeY, gridSizeZ];
        Vector3 worldBottomLeft = transform.position;

        //Creo y guardo los tiles
        for (int y = 0; y < gridSizeY; y++)
        {
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    Vector3 worldPoint = worldBottomLeft + Vector3.up * (y * nodeDiameter + nodeRadius) + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (z * nodeDiameter + nodeRadius);
                    bool isObstacle = false;
                    bool empty = false;
                    bool noTileInThisColumn = false;
                    bool startingTile = false;

                    //Si es la primera fila (el suelo vamos) únicamente compruebo obstáculos
                    if (y == 0)
                    {
                        //Compruebo si es un tile en el que se puede colocar a un personaje
                        if (Physics.CheckSphere(worldPoint, nodeRadius, startignTileMask))
                        {
                            startingTile = true;
                        }

                        else if (Physics.CheckSphere(worldPoint, nodeRadius, obstacleMask))
                        {
                            empty = false;
                            isObstacle = true;
                            noTileInThisColumn = false;
                        }

                        else if (Physics.CheckSphere(worldPoint, nodeRadius, noTileHereMask))
                        {
                            empty = false;
                            isObstacle = true;
                            noTileInThisColumn = true;
                        }
                    }

                    //Si ya hay altura
                    else
                    {
                        //Compruebo si es un tile en el que se puede colocar a un personaje
                        if (Physics.CheckSphere(worldPoint, nodeRadius, startignTileMask))
                        {
                            startingTile = true;
                        }

                        //Compruebo si hay obstáculos
                        else if (Physics.CheckSphere(worldPoint, nodeRadius, obstacleMask))
                        {
                            empty = false;
                            isObstacle = true;
                            noTileInThisColumn = false;
                        }

                        //Compruebo si es un obstáculos que no tiene tiles encima
                        else if (Physics.CheckSphere(worldPoint, nodeRadius, noTileHereMask))
                        {
                            empty = false;
                            isObstacle = true;
                            noTileInThisColumn = true;
                        }

                        else if (grid3DNode[x, y - 1, z].noTilesInThisColumn)
                        {
                            empty = true;
                            isObstacle = false;
                            noTileInThisColumn = false;
                        }

                        //Si no compruebo si en el tile de abajo hay un obstáculo
                        else if (grid3DNode[x, y - 1, z].isObstacle && !grid3DNode[x, y - 1, z].isEmpty)
                        {
                            empty = false;
                            isObstacle = false;
                            noTileInThisColumn = false;
                        }

                        //Si no se da ningúno de los dos casos entonces es un tile vacío
                        else
                        {
                            empty = true;
                            isObstacle = false;
                            noTileInThisColumn = false;
                        }
                    }

                    gridObject[x, y, z] = Instantiate(tilePref, new Vector3(worldPoint.x, worldPoint.y - 0.5f, worldPoint.z), Quaternion.identity);

                    gridObject[x, y, z].AddComponent<IndividualTiles>();

                    gridObject[x, y, z].GetComponent<IndividualTiles>().SetVariables(isObstacle, empty, noTileInThisColumn, startingTile, worldPoint, x, y, z, tilePref, LM, availableForMovementColor, currentTileHoverMovementColor, attackColor, actionRangeColor, chargingAttackColor);

                    grid3DNode[x, y, z] = gridObject[x, y, z].GetComponent<IndividualTiles>();
                }
            }
        }

        Create2DGrid();

    }

    //Una vez he creado el array de 3 dimensiones con todos los tiles, lo voy a transformar en un array de 2 donde solo guardo la x y la z.
    //La altura va como variable extra que no se usa dentro del array o de las coordenadas.
    void Create2DGrid()
    {
        grid2DNode = new IndividualTiles[gridSizeX, gridSizeZ];

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    //Añado al grid2D únicamente los tiles por los que se puede mover unidades
                    if (!grid3DNode[x, y, z].isEmpty && !grid3DNode[x, y, z].isObstacle && !grid3DNode[x, y, z].noTilesInThisColumn)
                    {
                        grid2DNode[x, z] = grid3DNode[x, y, z];
                        break;
                    }

                    if (y == gridSizeY-1)
                    {
                        grid2DNode[x, z] = grid3DNode[x, y, z];
                        break;
                    }
                }
            }
        }

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    if (grid3DNode[x, y, z].isEmpty || (grid3DNode[x, y, z].isObstacle && !grid3DNode[x, y, z].noTilesInThisColumn))
                    {
                        Destroy(gridObject[x, y, z].gameObject);
                    }
                }
            }
        }

        SetTilesNeighbours();
    }

    //NO ESTÁ PENSADO PARA QUE HAYA TILES ENCIMA DE OTROS A DIFERENTES ALTURAS.
    void SetTilesNeighbours()
    {
        for (int z = 0; z < gridSizeZ; z++)
        {
            for (int x = 0; x < gridSizeX; x++)
            {
                #region Izquierda
                if (x > 0)
                {
                    //Compruebo todos los tiles en linea
                    for (int k = 1; x - k >= 0; k++)
                    {
                        if (k == 1)
                        {
                            grid2DNode[x, z].neighbours.Add(grid2DNode[x - 1, z]);
                        }

                        grid2DNode[x, z].tilesInLineLeft.Add(grid2DNode[x - k, z]);
                    }
                }

                #endregion

                #region Derecha

                if (x < gridSizeX - 1)
                {
                    //Compruebo todos los tiles en linea
                    for (int k = 1; k < gridSizeX - x; k++)
                    {
                        if (k == 1)
                        {
                            grid2DNode[x, z].neighbours.Add(grid2DNode[x + 1, z]);
                        }

                        grid2DNode[x, z].tilesInLineRight.Add(grid2DNode[x + k, z]);

                    }
                }

                #endregion

                #region Abajo

                if (z > 0)
                {
                    //Compruebo todos los tiles en linea
                    for (int k = 1; z - k >= 0; k++)
                    {
                        if (k == 1)
                        {
                            grid2DNode[x, z].neighbours.Add(grid2DNode[x, z - 1]);
                        }

                        grid2DNode[x, z].tilesInLineDown.Add(grid2DNode[x, z - k]);
                    }
                }

                #endregion

                #region Arriba

                if (z < gridSizeZ - 1)
                {
                    //Compruebo todos los tiles en linea
                    for (int k = 1; k < gridSizeZ - z; k++)
                    {
                        if (k == 1)
                        {
                            grid2DNode[x, z].neighbours.Add(grid2DNode[x, z + 1]);
                        }
                        grid2DNode[x, z].tilesInLineUp.Add(grid2DNode[x, z + k]);
                    }
                }

                #endregion
            }
        }
    }

    #endregion

    //public IndividualTiles NodeFromWorldPoint(Vector3 worldPosition)
    //{
    //    float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
    //    float percentZ = (worldPosition.z + gridWorldSize.z / 2) / gridWorldSize.z;
    //    float percentY = (worldPosition.y + gridWorldSize.y / 2) / gridWorldSize.y;

    //    percentX = Mathf.Clamp01(percentX);
    //    percentZ = Mathf.Clamp01(percentZ);
    //    percentY = Mathf.Clamp01(percentY);

    //    int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
    //    int z = Mathf.RoundToInt((gridSizeZ - 1) * percentZ);
    //    int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
    //    return grid2DNode[x, z];
    //}

    #region PATHFINDING

    //Calculo el coste de una casilla
    float CostToEnterTile(int x, int z)
    {
        return grid2DNode[x, z].movementCost;
    }

    //Calculo tiles a los que se puede mover una unidad o en los que puede un enemigo buscar objetivos
    public List<IndividualTiles> OptimizedCheckAvailableTilesForMovement(int movementUds, UnitBase selectedUnit)
    {
        selectedCharacter = selectedUnit;
        tilesAvailableForMovement.Clear();

        //Recorro de izquierda a derecha los tiles que pueden estar disponibles para moverse (Va moviendose en X columna a columna)
        for (int i = -movementUds; i < (movementUds * 2) + 1; i++)
        {
            //Al restar a losMovementUds el i actual obtengo los tiles que hay por encima de la posición del personaje en dicha columna
            //Este número me sirve para calcular la posición en z de los tiles
            int tilesInZ = movementUds - Mathf.Abs(i);

            //Esto significa que es el extremo del rombo y sólo hay 1 tile en vertical
            if (tilesInZ == 0)
            {
                //Compruebo si existe un tile con esas coordenadas
                if (selectedCharacter.myCurrentTile.tileX + i < gridSizeX && selectedCharacter.myCurrentTile.tileX + i >= 0 &&
                    selectedCharacter.myCurrentTile.tileZ < gridSizeZ && selectedCharacter.myCurrentTile.tileZ >= 0)
                {

                    currentTileCheckingForMovement = grid2DNode[selectedCharacter.myCurrentTile.tileX + i, selectedCharacter.myCurrentTile.tileZ];

                    //Compruebo si el tile está ocupado, tiene un obstáculo o es un tile vacío
                    //IMPORTANTE NO COMPROBAR LA ALTURA. ESO SE HACE EN EL PATHFINDING. La altura se tiene que comprobar de un tile respecto a sus vecinos, no tiene sentido comprobar el tile en el que esta el player con el que quiere llegar.
                    if (currentTileCheckingForMovement != null && !currentTileCheckingForMovement.isEmpty && !currentTileCheckingForMovement.isObstacle)
                    {
                        //El enemigo no puede excluir los tiles que tienen personajes de jugador porque los necesita para encontrar el número de objetivos.
                        //Para que no se pinten sus tiles en la propia función de pintar he puesto un if que evita que se pintan.
                        if (currentTileCheckingForMovement.unitOnTile != null)
                        {
                            if (selectedCharacter.GetComponent<EnemyUnit>() && currentTileCheckingForMovement.unitOnTile.GetComponent<EnemyUnit>())
                            {
                                continue;
                            }

                            else if (selectedCharacter.GetComponent<PlayerUnit>())
                            {
                                continue;
                            }
                        }
                        
                        CalculatePathForMovementCost(currentTileCheckingForMovement.tileX, currentTileCheckingForMovement.tileZ);

                        if (tempCurrentPathCost <= movementUds)
                        {
                            tilesAvailableForMovement.Add(currentTileCheckingForMovement);
                        }

                        tempCurrentPathCost = 0;
                    }
                }
            }
            else
            {
                for (int j = tilesInZ; j >= -tilesInZ; j--)
                {
                    //Compruebo si existe un tile con esas coordenadas
                    if (selectedCharacter.myCurrentTile.tileX + i < gridSizeX && selectedCharacter.myCurrentTile.tileX + i >= 0 &&
                        selectedCharacter.myCurrentTile.tileZ + j < gridSizeZ && selectedCharacter.myCurrentTile.tileZ + j >= 0)
                    {

                        //Almaceno el tile en una variable
                        currentTileCheckingForMovement = grid2DNode[selectedCharacter.myCurrentTile.tileX + i, selectedCharacter.myCurrentTile.tileZ + j];

                        //Compruebo si el tile está ocupado, tiene un obstáculo o es un tile vacío
                        //IMPORTANTE NO COMPROBAR LA ALTURA. ESO SE HACE EN EL PATHFINDING. La altura se tiene que comprobar de un tile respecto a sus vecinos, no tiene sentido comprobar el tile en el que esta el player con el que quiere llegar.
                        if (currentTileCheckingForMovement != null && !currentTileCheckingForMovement.isEmpty && !currentTileCheckingForMovement.isObstacle)
                        {
                            //El enemigo no puede excluir los tiles que tienen personajes de jugador porque los necesita para encontrar el número de objetivos.
                            //Para que no se pinten sus tiles en la propia función de pintar he puesto un if que evita que se pintan.
                            if (currentTileCheckingForMovement.unitOnTile != null)
                            {
                                if (selectedCharacter.GetComponent<EnemyUnit>() && currentTileCheckingForMovement.unitOnTile.GetComponent<EnemyUnit>())
                                {
                                    continue;
                                }

                                else if (selectedCharacter.GetComponent<PlayerUnit>())
                                {
                                    continue;
                                }
                            }

                            //Compruebo si existe un camino hasta el tile
                            CalculatePathForMovementCost(currentTileCheckingForMovement.tileX, currentTileCheckingForMovement.tileZ);
                            if (tempCurrentPathCost <= movementUds)
                            {
                                tilesAvailableForMovement.Add(currentTileCheckingForMovement);
                            }

                            tempCurrentPathCost = 0;
                        }
                    }
                }
            }
        }
       
        return tilesAvailableForMovement;
    }

    public void CalculatePathForMovementCost(int x, int z)
    {
        openList.Clear();
        closedHasSet.Clear();

        //Origen y target
        source = grid2DNode[selectedCharacter.myCurrentTile.tileX, selectedCharacter.myCurrentTile.tileZ];
        target = grid2DNode[x, z];

        //Debug.Log("SOURCE " + source.name);
        //Debug.Log("TARGET " + target.name);

        openList.Add(source);

        //Mientras que haya nodos que no hayan sido visitados...
        while (openList.Count > 0)
        {
            IndividualTiles currentNode = openList[0];

            for (int i = 0; i < openList.Count; i++)
            {
                if (openList[i].CalculateFCost < currentNode.CalculateFCost ||
                    openList[i].CalculateFCost == currentNode.CalculateFCost && openList[i].hCost < currentNode.hCost)
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedHasSet.Add(currentNode);

            #region LAST_PART

            //Si el nodo coincide con el objetivo, terminamos la busqueda.
            if (currentNode == target)
            {
                currentPath.Clear();
                //Si llega hasta aquí si que hay un camino hasta el objetivo.
                curr = target;

                //Recorre la cadena de Prev y la añade a la lista que guarda el camino.
                //Esta ruta está al reves, va desde el objetivo hasta el origen.
                while (curr != null)
                {
                    if (!currentPath.Contains(curr))
                    {
                        currentPath.Add(curr);
                        curr = curr.parent;
                    }

                    else
                    {
                        Debug.LogError("ERROR DE LOOP. TILES REPETIDOS EN EL CURRENTPATH");

                        for (int i = 0; i < currentPath.Count; i++)
                        {
                            Debug.Log(currentPath[i].name);
                        }

                        Debug.Log(curr.name);
                        break;
                    }
                }

                //Le damos la vuelta a la lista para que vaya desde el orgien hasta el objetivo.
                currentPath.Reverse();

                //Calcular coste del path
                for (int i = 0; i < currentPath.Count; i++)
                {
                    //Sumo el coste de todas las casillas que forman el path excepto la primera (ya que es la casilla sobre la que se encuentra la unidad).
                    if (i != 0)
                    {
                        tempCurrentPathCost += CostToEnterTile(currentPath[i].tileX, currentPath[i].tileZ);
                    }
                }

                for (int i = 0; i < gridSizeX; i++)
                {
                    for (int j = 0; j < gridSizeZ; j++)
                    {
                        grid2DNode[i, j].ClearPathfindingVariables();
                    }
                }

                return;
            }

            #endregion

            foreach (IndividualTiles neighbour in grid2DNode[currentNode.tileX, currentNode.tileZ].neighbours)
            {
                //Goblin
                if (selectedCharacter.GetComponent<EnGoblin>())
                {
                    if (neighbour == null || neighbour.isEmpty || neighbour.isObstacle || closedHasSet.Contains(neighbour) || Mathf.Abs(neighbour.height - grid2DNode[currentNode.tileX, currentNode.tileZ].height) > selectedCharacter.maxHeightDifferenceToMove)
                    {
                        //if (neighbour.unitOnTile != null)
                        //{
                        //    Debug.Log( "First" + neighbour.unitOnTile.name);
                        //}
                      
                        continue;
                    }

                    //Exceptuando el target que siempre va a tener una unidad, compruebo si los tiles para formar el path no están ocupados por enemigos
                    else if (neighbour != target && neighbour.unitOnTile != null || neighbour.unitOnTile != null && neighbour.unitOnTile.GetComponent<EnemyUnit>())
                    {
                        //if (neighbour.unitOnTile != null)
                        //{
                        //    Debug.Log("Second" + neighbour.unitOnTile.name);
                        //}
                        continue;
                    }                 
                }

                //Player
                if ((selectedCharacter.GetComponent<PlayerUnit>() && (neighbour == null || neighbour.isEmpty || neighbour.isObstacle || neighbour.unitOnTile != null)) || Mathf.Abs(neighbour.height - grid2DNode[currentNode.tileX, currentNode.tileZ].height) > selectedCharacter.maxHeightDifferenceToMove || closedHasSet.Contains(neighbour))
                {
                    continue;
                }

                //El gigante se tiene en cuenta al no ponerle condiciones de tile vacio o tile obstaculo

                int newMovemntCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);

                if (newMovemntCostToNeighbour < neighbour.gCost || !openList.Contains(neighbour))
                {
                    neighbour.gCost = newMovemntCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, target);

                    neighbour.parent = currentNode;

                    if (!openList.Contains(neighbour))
                    {
                        openList.Add(neighbour);
                    }
                }
            }

            //Si llega hasta aqui significa que no hay tiles a los que moverse por lo que el coste es infinito.
            //NO PONER RETURN, puede darse el caso de que este llegando al ultimo tile de una fila y si estan todos los lados bloqueados menos el tile anterior,
            //entraria aqui porque el tile anterior no puede revisitarlo, asi que cortariamos el proceso de seguir comprobando el resto de tiles de Open antes de tiempo.
            if (openList.Count == 0)
            {
                //Debug.Log("No hay tiles a los que me pueda mover");
                tempCurrentPathCost = Mathf.Infinity;
            }
        }
    }

    int GetDistance(IndividualTiles nodeA, IndividualTiles nodeB)
    {
       return(Mathf.Abs(nodeA.tileX - nodeB.tileX) + Mathf.Abs(nodeA.tileZ - nodeB.tileZ));
    }

    #endregion

    #region ENEMY_PATHFINDING

    //Estas funciones son bastante parecidas a las del pathfinding normal salvo que devuelven personajes a los que pueden atacar en vez de tiles.
    //El gigante llama a esta función pero no a las anteriores.
    //Sin embargo el goblin llama a esta por el objetivo y a las anteriores por el path

    //Doy feedback de que casillas están al alcance del personaje.
    public List<UnitBase> checkAvailableCharactersForAttack(int range, EnemyUnit currentEnemy)
    {
        charactersAvailableForAttack.Clear();
        tempCurrentObjectiveCost = 0;
        tempCurrentPathCost = 0;

        //Reuno en una lista todos los tiles a los que puedo acceder
        OptimizedCheckAvailableTilesForMovement(range, currentEnemy);

        for (int i = 0; i < tilesAvailableForMovement.Count; i++)
        {
            if (tilesAvailableForMovement[i].unitOnTile != null && tilesAvailableForMovement[i].unitOnTile.GetComponent<PlayerUnit>())
            {
                CalculatePathForMovementCost(tilesAvailableForMovement[i].unitOnTile.myCurrentTile.tileX, tilesAvailableForMovement[i].unitOnTile.myCurrentTile.tileZ);

                //Guardar el tempcurrentPathcost en otra variable y usarlo para comparar
                if (tempCurrentObjectiveCost == 0 || tempCurrentObjectiveCost >= tempCurrentPathCost)
                {
                    //Si se da el caso que temCurrentPathCost es 0 significa que no ha encontrado un camino hasta el enemigo (creo)
                    if (tempCurrentPathCost != 0)
                    {
                        if (tempCurrentObjectiveCost > tempCurrentPathCost)
                        {
                            //Limpio la lista de objetivos y añado
                            charactersAvailableForAttack.Clear();
                        }

                        //Me guardo la distancia para checkear
                        tempCurrentObjectiveCost = tempCurrentPathCost;

                        charactersAvailableForAttack.Add(tilesAvailableForMovement[i].unitOnTile);
                    }
                }
                //Resetear tempcurrentPathCost a 0
                tempCurrentPathCost = 0;
            }
        }
        //Reset


        return charactersAvailableForAttack;
    }
    #endregion

    public List<UnitBase> OnlyCheckClosestPathToPlayer()
    {
        charactersAvailableForAttack.Clear();
        tempCurrentObjectiveCost = 0;
        tempCurrentPathCost = 0;

        for (int i = 0; i < LM.characthersOnTheBoard.Count; i++)
        {
            CalculatePathForMovementCost(LM.characthersOnTheBoard[i].myCurrentTile.tileX, LM.characthersOnTheBoard[i].myCurrentTile.tileZ);

            print(tempCurrentPathCost);
            print(LM.characthersOnTheBoard[i].myCurrentTile);

            if (tempCurrentObjectiveCost == 0 || tempCurrentObjectiveCost >= tempCurrentPathCost)
            {
                //Si se da el caso que temCurrentPathCost es 0 significa que no ha encontrado un camino hasta el enemigo (creo)
                if (tempCurrentPathCost != 0)
                {
                    if (tempCurrentObjectiveCost > tempCurrentPathCost)
                    {
                        //Limpio la lista de objetivos y añado
                        charactersAvailableForAttack.Clear();
                    }

                    //Me guardo la distancia para checkear
                    tempCurrentObjectiveCost = tempCurrentPathCost;

                    charactersAvailableForAttack.Add(LM.characthersOnTheBoard[i]);
                }
            }

            tempCurrentPathCost = 0;
        }

        //Reset
        return charactersAvailableForAttack;
    }
                
    //Función que sirve para encontrar a todos los enemigos en rango y que el goblin pueda alertarlos
    public List<UnitBase> GetAllUnitsInRangeWithoutPathfinding(int rangeToCheck, UnitBase selectedUnit)
    {
        unitsInRangeWithoutPathfinding.Clear();

        //Recorro de izquierda a derecha los tiles que pueden estar disponibles para moverse (Va moviendose en X columna a columna)
        for (int i = -rangeToCheck; i < (rangeToCheck * 2) + 1; i++)
        {
            //Al restar a losMovementUds el i actual obtengo los tiles que hay por encima de la posición del personaje en dicha columna
            //Este número me sirve para calcular la posición en z de los tiles
            int tilesInZ = rangeToCheck - Mathf.Abs(i);

            //Esto significa que es el extremo del rombo y sólo hay 1 tile en vertical
            if (tilesInZ == 0)
            {
                //Compruebo si existe un tile con esas coordenadas
                if (selectedUnit.myCurrentTile.tileX + i < gridSizeX && selectedUnit.myCurrentTile.tileX + i >= 0 &&
                    selectedUnit.myCurrentTile.tileZ < gridSizeZ && selectedUnit.myCurrentTile.tileZ >= 0)
                {
                    currentTileCheckingForUnit = grid2DNode[selectedUnit.myCurrentTile.tileX + i, selectedUnit.myCurrentTile.tileZ];

                    if (currentTileCheckingForUnit != null && currentTileCheckingForUnit.unitOnTile != null && currentTileCheckingForUnit.unitOnTile.GetComponent<UnitBase>())
                    {
                        if (currentTileCheckingForUnit.unitOnTile.GetComponent<UnitBase>() != selectedUnit)
                        {
                            unitsInRangeWithoutPathfinding.Add(currentTileCheckingForUnit.unitOnTile.GetComponent<UnitBase>());
                        }
                    }
                }
            }
            else
            {
                for (int j = tilesInZ; j >= -tilesInZ; j--)
                {
                    //Compruebo si existe un tile con esas coordenadas
                    if (selectedUnit.myCurrentTile.tileX + i < gridSizeX && selectedUnit.myCurrentTile.tileX + i >= 0 &&
                        selectedUnit.myCurrentTile.tileZ + j < gridSizeZ && selectedUnit.myCurrentTile.tileZ + j >= 0)
                    {
                        //Almaceno el tile en una variable
                        currentTileCheckingForUnit = grid2DNode[selectedUnit.myCurrentTile.tileX + i, selectedUnit.myCurrentTile.tileZ + j];

                        if (currentTileCheckingForUnit != null && currentTileCheckingForUnit.unitOnTile != null && currentTileCheckingForUnit.unitOnTile.GetComponent<UnitBase>())
                        {
                            if (currentTileCheckingForUnit.unitOnTile.GetComponent<UnitBase>() != selectedUnit)
                            {
                                unitsInRangeWithoutPathfinding.Add(currentTileCheckingForUnit.unitOnTile.GetComponent<UnitBase>());
                            }
                        }
                    }
                }
            }
        }

        return unitsInRangeWithoutPathfinding;
    }

    //Esta función es una copia de CheckAvailableTiles for Movement solo que no calcula el pathfinding de los tiles, simplemente los pinta.
    //Esto sirve para que el rango de los enemigos no dependa de pathfinding ya que siempre es el mismo
    public List<IndividualTiles> CheckAvailableTilesForEnemyAction(int movementUds, EnemyUnit selectedUnit)
    {
        tilesAvailableForEnemyAction.Clear();

        //Recorro de izquierda a derecha los tiles que pueden estar disponibles para moverse (Va moviendose en X columna a columna)
        for (int i = -movementUds; i < (movementUds * 2) + 1; i++)
        {
            //Al restar a losMovementUds el i actual obtengo los tiles que hay por encima de la posición del personaje en dicha columna
            //Este número me sirve para calcular la posición en z de los tiles
            int tilesInZ = movementUds - Mathf.Abs(i);

            for (int j = tilesInZ; j >= -tilesInZ; j--)
            {

                //Compruebo si existe un tile con esas coordenadas
                if (selectedUnit.myCurrentTile.tileX + i < gridSizeX && selectedUnit.myCurrentTile.tileX + i >= 0 &&
                    selectedUnit.myCurrentTile.tileZ + j < gridSizeZ && selectedUnit.myCurrentTile.tileZ + j >= 0)
                {
                    if (!grid2DNode[selectedUnit.myCurrentTile.tileX + i, selectedUnit.myCurrentTile.tileZ + j].isObstacle && !grid2DNode[selectedUnit.myCurrentTile.tileX + i, selectedUnit.myCurrentTile.tileZ + j].isEmpty)
                    {
                        tilesAvailableForEnemyAction.Add(grid2DNode[selectedUnit.myCurrentTile.tileX + i, selectedUnit.myCurrentTile.tileZ + j]);
                    }
                }
            }
        }

        return tilesAvailableForEnemyAction;
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(new Vector3 (transform.position.x + gridWorldSize.x/2, transform.position.y + gridWorldSize.y / 2, transform.position.z + gridWorldSize.z / 2), new Vector3(gridWorldSize.x, gridWorldSize.y, gridWorldSize.z));
    }
}
