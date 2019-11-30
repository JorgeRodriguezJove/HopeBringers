﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour

{
    #region VARIABLES

    public LayerMask unwalkableMask;
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
    List<IndividualTiles> tilesAvailableForMovement = new List<IndividualTiles>();

    [Header("ENEMY_PATHFINDING")]

    ////Enemigo actual que está calculando su pathfinding
    //private EnemyUnit selectedEnemy;

    //Lista de posibles objetivos del enemigo actual
    List<UnitBase> charactersAvailableForAttack = new List<UnitBase>();

    //Variable que se usa para almacenar la distancia con el objetivo
    float tempCurrentObjectiveCost;

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
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.up * gridWorldSize.y / 2 - Vector3.forward * gridWorldSize.z / 2;

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
                    if (y == 0)
                    {
                        isObstacle = (Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));
                    }

                    else
                    {
                        if (Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask))
                        {
                            empty = false;
                            isObstacle = true;
                        }

                        else if (grid3DNode[x, y - 1, z].isObstacle && !grid3DNode[x, y - 1, z].isEmpty)
                        {
                            empty = false;
                            isObstacle = false;
                        }

                        else
                        {
                            empty = true;
                            isObstacle = false;
                        }
                    }

                    gridObject[x, y, z] = Instantiate(tilePref, new Vector3(worldPoint.x, worldPoint.y - 0.5f, worldPoint.z), Quaternion.identity);

                    gridObject[x, y, z].AddComponent<IndividualTiles>();

                    gridObject[x, y, z].GetComponent<IndividualTiles>().SetVariables(isObstacle, empty, worldPoint, x, y, z, tilePref, LM);

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
                    if (!grid3DNode[x, y, z].isEmpty && !grid3DNode[x, y, z].isObstacle)
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
                    if (grid3DNode[x, y, z].isEmpty || grid3DNode[x, y, z].isObstacle)
                    {
                        Destroy(gridObject[x, y, z].gameObject);
                    }

                }
            }
        }

        SetTilesNeighbours();
    }


    ////int prevY;

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

    public IndividualTiles NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentZ = (worldPosition.z + gridWorldSize.z / 2) / gridWorldSize.z;
        float percentY = (worldPosition.y + gridWorldSize.y / 2) / gridWorldSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentZ = Mathf.Clamp01(percentZ);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int z = Mathf.RoundToInt((gridSizeZ - 1) * percentZ);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid2DNode[x, z];
    }



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
                    if (!currentTileCheckingForMovement.isEmpty && !currentTileCheckingForMovement.isObstacle)
                    {
                        if (selectedCharacter.GetComponent<EnemyUnit>() || (selectedCharacter.GetComponent<PlayerUnit>() && currentTileCheckingForMovement.unitOnTile == null))
                        {
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
                        if (!currentTileCheckingForMovement.isEmpty && !currentTileCheckingForMovement.isObstacle)
                        {
                            if (selectedCharacter.GetComponent<EnemyUnit>() || (selectedCharacter.GetComponent<PlayerUnit>() && currentTileCheckingForMovement.unitOnTile == null))
                            {
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
        }

        return tilesAvailableForMovement;
    }

    public void CalculatePathForMovementCost(int x, int z)
    {
        currentPath.Clear();
        unvisited.Clear();

        //Origen y target
        source = grid2DNode[selectedCharacter.myCurrentTile.tileX, selectedCharacter.myCurrentTile.tileZ];
        target = grid2DNode[x, z];

        //La distancia que hay desde el origen hasta el origen es 0. Por lo que en el diccionario, el nodo que coincida con el origen, su float valdrá 0.
        dist[source] = 0;
        //No hay ningún nodo antes que el origen por lo que el valor de source en el diccionario es null.
        prev[source] = null;

        //Inicializamos para que pueda llegar hasta alcance infinito ya que no se la distancia hasta el objetivo. Al ponerlos todos en infinitos menos el source, me aseguro que empieza desde ahí.
        //En principio no llegará nunca hasta el infinito porque encontrará antes el objetivo y entonces se cortará el proceso.
        //También sirve para contemplar las casillas a las que no se puede llegar (es cómo si tuviesen valor infinito).
        foreach (IndividualTiles node in grid2DNode)
        {
            //Si el nodo no ha sido quitado de los nodos sin visitar
            if (node != source)
            {
                dist[node] = Mathf.Infinity;
                prev[node] = null;
            }

            //Todos los nodos se añaden a la lista de unvisited, incluido el origen.
            unvisited.Add(node);
        }

        //Mientras que haya nodos que no hayan sido visitados...
        while (unvisited.Count > 0)
        {
            //currentNode se corresponde con el nodo no visitado con la distancia más corta
            //La primera vez va a ser source ya que es el único nodo que no tiene valor infinito
            //Después de eso sólo podrá coger una de las casillas vecinas y así irá repitiendo el ciclo.
            IndividualTiles currentNode = null;

            foreach (IndividualTiles possibleNode in unvisited)
            {
                if (currentNode == null || dist[possibleNode] < dist[currentNode])
                {
                    currentNode = possibleNode;
                }
            }

            //Si el nodo coincide con el objetivo, terminamos la busqueda.
            if (currentNode == target)
            {
                break;
            }

            unvisited.Remove(currentNode);

            foreach (IndividualTiles node in currentNode.neighbours)
            {
                if (selectedCharacter.GetComponent<EnGiant>())
                {
                    float alt = dist[currentNode] + CostToEnterTile(node.tileX, node.tileZ);

                    if (alt < dist[node])
                    {
                        if (Mathf.Abs(node.height - currentNode.height) <= selectedCharacter.maxHeightDifferenceToMove)
                        {
                            dist[node] = alt;
                            prev[node] = currentNode;
                        }
                    }
                }

                else if (selectedCharacter.GetComponent<EnGoblin>())
                {
                    //Si el nodo no está vacío o un obstáculo puedo seguir comprobando el path
                    if (!node.isEmpty && !node.isObstacle)
                    {
                        //Exceptuando el target que siempre va a tener una unidad, compruebo si los tiles para formar el path no están ocupados por enemigos
                        if ((node != target && node.unitOnTile == null) || node == target)
                        {
                            float alt = dist[currentNode] + CostToEnterTile(node.tileX, node.tileZ);

                            if (alt < dist[node])
                            {
                                if (Mathf.Abs(node.height - currentNode.height) <= selectedCharacter.maxHeightDifferenceToMove)
                                {
                                    dist[node] = alt;
                                    prev[node] = currentNode;
                                }
                            }
                        }
                    }
                }

                else
                {
                    if (node.unitOnTile == null && !node.isEmpty && !node.isObstacle)
                    {
                        float alt = dist[currentNode] + CostToEnterTile(node.tileX, node.tileZ);

                        if (alt < dist[node])
                        {
                            if (Mathf.Abs(node.height - currentNode.height) <= selectedCharacter.maxHeightDifferenceToMove)
                            {
                                dist[node] = alt;
                                prev[node] = currentNode;
                            }
                        }
                    }
                }
            }
        }

        if (prev[target] == null)
        {
            //Si llega aquí significa que no hay ninguna ruta disponible desde el origen hasta el objetivo.
            tempCurrentPathCost = Mathf.Infinity;
        }

        //Si llega hasta aquí si que hay un camino hasta el objetivo.
        curr = target;

        //Recorre la cadena de Prev y la añade a la lista que guarda el camino.
        //Esta ruta está al reves, va desde el objetivo hasta el origen.
        while (curr != null)
        {
            currentPath.Add(curr);
            curr = prev[curr];
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
                    if (tempCurrentObjectiveCost > tempCurrentPathCost)
                    {
                        //Limpio la lista de objetivos y añado
                        charactersAvailableForAttack.Clear();
                    }
                    //Me guardo la distancia para checkear
                    tempCurrentObjectiveCost = tempCurrentPathCost;

                    charactersAvailableForAttack.Add(tilesAvailableForMovement[i].unitOnTile);
                }

                //Resetear tempcurrentPathCost a 0
                tempCurrentPathCost = 0;
            }
        }
        //Reset


        return charactersAvailableForAttack;
    }
    #endregion



    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, gridWorldSize.y, gridWorldSize.z));
    }
}
