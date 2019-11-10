﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    #region VARIABLES

    [Header("CREACIÓN DE MAPA")]

    //Array donde se meten los tiles en el editor
    [SerializeField]
    public GameObject[] tilesInScene;

    //2D array con las coordenadas de los tiles. (Básicamente convierte el array tilesInScene en un array 2D)
    private GameObject[,] tilesCoord;

    [SerializeField]
    public int mapSizeX;
    [SerializeField]
    public int mapSizeZ;

    //Array con script de tiles que voy a usar para calcular el pathfinding
    [HideInInspector]
    public IndividualTiles[,] graph;

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

    //Lista de tiles que forman el path desde un tile hasta otro. Al igual que temCurrentPathCost se resetea cada vez que se llama a la función CalculatePathForMovement
    [HideInInspector]
    public List<IndividualTiles> currentPath = new List<IndividualTiles>();

    //Si es true se mueve en diagonal, si no se mueve en torre.
    [SerializeField]
    public bool isDiagonalMovement;
    [SerializeField]
    public bool isChooseRotationIfTower;

    //Personaje actualmente seleccionado
    private UnitBase selectedCharacter;

    //Tiles que se puede mover el personaje seleccionado
    private int mxMovementUdsSelectedCharacter;

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

    private LevelManager LM;

    #endregion

    #region INIT

    private void Awake()
    {
        LM = FindObjectOfType<LevelManager>();
        SaveTilePosition();
        GeneratePathFindingGraph();
    }

 
    //Ordeno el array tilesInScene con los 100 tiles en un array 2D 10x10
    void SaveTilePosition()
    {
        tilesCoord = new GameObject[mapSizeX, mapSizeZ];

        int k = 0;
        for (int i = 0; i < mapSizeZ; i++)
        {
            
            for (int j = 0; j < mapSizeX; j++)
            {
                tilesCoord[j, i] = tilesInScene[k];
                k++;
            }
        }
    }

    //Genero el graph con los nodos que voy a usar para calcular el pathfinding.
    void GeneratePathFindingGraph()
    {
        //Inicializo el array
        graph = new IndividualTiles[mapSizeX, mapSizeZ];

        //Obtengo una referencia del script de cada tile, lo guardo en la lista y le paso sus coordenadas y una referncia al GM
        for (int i = 0; i < mapSizeZ; i++)
        {
            for (int j = 0; j < mapSizeX; j++)
            {
                graph[j, i] = tilesCoord[j, i].GetComponent<IndividualTiles>();
                graph[j, i].GetComponent<IndividualTiles>().TM = this;
                graph[j, i].GetComponent<IndividualTiles>().LM = LM;
                graph[j, i].tileX = j;
                graph[j, i].tileZ = i;
            }
        }

        //Una vez que todos los tiles en el array existen y saben sus coordenadas, calculo los nodos vecinos y se los paso a cada tile.
        for (int i = 0; i < mapSizeZ; i++)
        {
            for (int j = 0; j < mapSizeX; j++)

            {
                //Casilla vecina de la izquierda
                if (j > 0)
                {
                    graph[j, i].neighbours.Add(graph[j - 1, i]);

                    for (int k = 1; j - k >= 0 ; k++)
                    {
                        graph[j, i].tilesInLineLeft.Add(graph[j - k, i]);
                    }
                }

                //Casilla vecina de la derecha
                if (j < mapSizeX - 1)
                {
                    graph[j, i].neighbours.Add(graph[j + 1, i]);

                    for (int k = 1; k < mapSizeX - j ; k++)
                    {
                        graph[j, i].tilesInLineRight.Add(graph[j + k, i]);
                    }
                }

                //Casilla vecina de abajo
                if (i > 0)
                {
                    graph[j, i].neighbours.Add(graph[j, i - 1]);

                    for (int k = 1; i - k >= 0; k++)
                    {
                        graph[j, i].tilesInLineDown.Add(graph[j, i- k]);
                    }
                }

                //Casilla vecina de arriba
                if (i < mapSizeZ - 1)
                {
                    graph[j, i].neighbours.Add(graph[j, i + 1]);

                    for (int k = 1; k < mapSizeZ - i; k++)
                    {
                        graph[j, i].tilesInLineUp.Add(graph[j, i + k]);
                    }
                }
            }
        }
    }

    #endregion

    #region PATHFINDING

    //Calculo el coste de una casilla
    float CostToEnterTile(int x, int z)
    {
        return graph[x, z].MovementCost;
    }

    //Calculo tiles a los que se puede mover una unidad o en los que puede un enemigo buscar objetivos
    public List<IndividualTiles> OptimizedCheckAvailableTilesForMovement(int movementUds, UnitBase selectedUnit)
    {
        selectedCharacter = selectedUnit;
        tilesAvailableForMovement.Clear();
        mxMovementUdsSelectedCharacter = selectedCharacter.movementUds;

        //Recorro de izquierda a derecha los tiles que pueden estar disponibles para moverse (Va moviendose en X columna a columna)
        for (int i = -mxMovementUdsSelectedCharacter; i < (mxMovementUdsSelectedCharacter * 2) + 1; i++)
        {
            //Al restar a losMovementUds el i actual obtengo los tiles que hay por encima de la posición del personaje en dicha columna
            //Este número me sirve para calcular la posición en z de los tiles
            int tilesInVertical = mxMovementUdsSelectedCharacter - Mathf.Abs(i);

            //Esto significa que es el extremo del rombo y sólo hay 1 tile en vertical
            if (tilesInVertical == 0)
            {
                //Compruebo si existe un tile con esas coordenadas
                if (selectedCharacter.myCurrentTile.tileX + i < mapSizeX && selectedCharacter.myCurrentTile.tileX + i >= 0 &&
                    selectedCharacter.myCurrentTile.tileZ < mapSizeZ && selectedCharacter.myCurrentTile.tileZ >= 0)
                {
                    //Almaceno el tile en una variable
                    currentTileCheckingForMovement = graph[selectedCharacter.myCurrentTile.tileX + i, selectedCharacter.myCurrentTile.tileZ];

                    //Compruebo si el tile está ocupado, tiene un obstáculo o es un tile vacío
                    if (!currentTileCheckingForMovement.isEmpty && !currentTileCheckingForMovement.isObstacle)
                    {
                        if (selectedCharacter.GetComponent<EnemyUnit>() || (selectedCharacter.GetComponent<PlayerUnit>() && currentTileCheckingForMovement.unitOnTile == null))
                        {
                            //Compruebo si existe un camino hasta el tile
                            CalculatePathForMovementCost(currentTileCheckingForMovement.tileX, currentTileCheckingForMovement.tileZ);
                            if (tempCurrentPathCost <= mxMovementUdsSelectedCharacter)
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
                for (int j = tilesInVertical; j >= -tilesInVertical; j--)
                {
                    //Compruebo si existe un tile con esas coordenadas
                    if (selectedCharacter.myCurrentTile.tileX + i < mapSizeX && selectedCharacter.myCurrentTile.tileX + i >= 0 &&
                        selectedCharacter.myCurrentTile.tileZ + j < mapSizeZ && selectedCharacter.myCurrentTile.tileZ + j >= 0)
                    {
                        //Almaceno el tile en una variable
                        currentTileCheckingForMovement = graph[selectedCharacter.myCurrentTile.tileX + i, selectedCharacter.myCurrentTile.tileZ + j];

                        //Compruebo si el tile está ocupado, tiene un obstáculo o es un tile vacío
                        if (!currentTileCheckingForMovement.isEmpty && !currentTileCheckingForMovement.isObstacle)
                        {
                            if (selectedCharacter.GetComponent<EnemyUnit>() || (selectedCharacter.GetComponent<PlayerUnit>() && currentTileCheckingForMovement.unitOnTile == null))
                            {
                                //Compruebo si existe un camino hasta el tile
                                CalculatePathForMovementCost(currentTileCheckingForMovement.tileX, currentTileCheckingForMovement.tileZ);
                                if (tempCurrentPathCost <= mxMovementUdsSelectedCharacter)
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


    //Calculo el path para ir hasta una casilla seleccionada desde la posición actual del personaje.
    public void CalculatePathForMovementCost(int x, int z)
    {
        currentPath.Clear();
        unvisited.Clear();

        //Origen y target
        source = graph[selectedCharacter.myCurrentTile.tileX, selectedCharacter.myCurrentTile.tileZ];
        target = graph[x, z];

        //La distancia que hay desde el origen hasta el origen es 0. Por lo que en el diccionario, el nodo que coincida con el origen, su float valdrá 0.
        dist[source] = 0;
        //No hay ningún nodo antes que el origen por lo que el valor de source en el diccionario es null.
        prev[source] = null;

        //Inicializamos para que pueda llegar hasta alcance infinito ya que no se la distancia hasta el objetivo. Al ponerlos todos en infinitos menos el source, me aseguro que empieza desde ahí.
        //En principio no llegará nunca hasta el infinito porque encontrará antes el objetivo y entonces se cortará el proceso.
        //También sirve para contemplar las casillas a las que no se puede llegar (es cómo si tuviesen valor infinito).
        foreach (IndividualTiles node in graph)
        {
            //Si el nodo no ha sido quitado de los nodos sin visitar
            if (node != source)
            {
                dist[node] = Mathf.Infinity;
                prev[node] = null;
            }

            //Todos los nodos se añaden a la lista de unvisited, incluido el origen.

            if (isDiagonalMovement || selectedCharacter.GetComponent<EnemyUnit>())
            {
                unvisited.Add(node);
            }

            else
            {
                if (node.tileX == selectedCharacter.GetComponent<UnitBase>().myCurrentTile.tileX || node.tileZ == selectedCharacter.GetComponent<UnitBase>().myCurrentTile.tileZ)
                {
                    unvisited.Add(node);
                }
            }
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
                    if (isDiagonalMovement || selectedCharacter.GetComponent<EnemyUnit>())
                    {
                        currentNode = possibleNode;
                    }

                    else
                    {
                        if (possibleNode.tileX == selectedCharacter.GetComponent<UnitBase>().myCurrentTile.tileX || possibleNode.tileZ == selectedCharacter.GetComponent<UnitBase>().myCurrentTile.tileZ)
                        {
                            currentNode = possibleNode;
                        }
                    }
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
                    if (!node.isEmpty && !node.isObstacle)
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

                else 
                {
                    if (node.unitOnTile == null && !node.isEmpty && !node.isObstacle)
                    {
                        if (isDiagonalMovement || (!isDiagonalMovement && (node.tileX == selectedCharacter.GetComponent<UnitBase>().myCurrentTile.tileX || node.tileZ == selectedCharacter.GetComponent<UnitBase>().myCurrentTile.tileZ)))
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
                    //Me guardo la distancia para checkear
                    tempCurrentObjectiveCost = tempCurrentPathCost;
                    //Limpio la lista de objetivos y añado
                    if (tempCurrentObjectiveCost > tempCurrentPathCost)
                    {
                        charactersAvailableForAttack.Clear();
                    }
                    charactersAvailableForAttack.Add(tilesAvailableForMovement[i].unitOnTile);
                }


                //Resetear tempcurrentPathCost a 0
                tempCurrentPathCost = 0;
            }
        }

        return charactersAvailableForAttack;
    }
    #endregion
}
