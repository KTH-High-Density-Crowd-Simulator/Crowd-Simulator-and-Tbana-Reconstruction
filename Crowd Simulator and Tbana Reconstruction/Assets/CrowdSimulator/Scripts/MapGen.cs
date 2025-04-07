using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapGen : MonoBehaviour {

	List<Vector3> f = new List<Vector3>();
	List<Vector3> b = new List<Vector3>();
	public static float DEFAULT_THRESHOLD = 0.5f;

	public struct map{
		public List<List<List<int>>> shortestPaths;
		public List<CustomNode> allNodes;
		public List<spawnNode> spawns;
		public List<int> goals;
	}

	public struct spawnNode {
		public int node;
		public Spawner spawner;
	}

	private map roadmap;

	public map getRoadmap()
	{
		return roadmap;
	}

	void sweepMap(Vector2 xMinMax, Vector2 zMinMax) {
		for (int i = 0; i < xMinMax.y - xMinMax.x; ++i) {
			for(int j = 0; j < zMinMax.y-zMinMax.x; ++j) {
				Vector3 pos = new Vector3(xMinMax.x + i, 0.0f, zMinMax.x + j);
				if (isFree(pos)) {
					f.Add (pos);
				} else {
					b.Add (pos);
				}
			}
		}
	}

	public bool isFree(Vector3 p) {
		return !Physics.Raycast(new Vector3 (p.x, 100, p.z), new Vector3 (0, -10, 0), 150f);
	}

	private Vector3 getClosestPoint(ref List<Vector3> li, Vector3 point) {
		float dis = float.PositiveInfinity;
		Vector3 closest = Vector3.zero;
		for (int i = 0; i < li.Count; ++i) {
			float newDist = (point - li [i]).magnitude;
			if (newDist < dis) {
				dis = newDist;
				closest = li [i];
			}
		}
		return closest;
	}
	private Vector3 getBorderDistance(Vector3 startPoint, Vector2 xMinMax, Vector2 zMinMax) {
		float dis = startPoint.x - xMinMax.x;
		Vector3 borderPoint = new Vector3 (xMinMax.x, startPoint.y, startPoint.z);
		if (xMinMax.y - startPoint.x < dis) {
			dis = xMinMax.y - startPoint.x;
			borderPoint.x = xMinMax.y;
		} 
		if (startPoint.z - zMinMax.x < dis) {
			dis = startPoint.z - zMinMax.x;
			borderPoint.x = startPoint.x; borderPoint.z = zMinMax.x;
		}
		if (zMinMax.y - startPoint.z < dis) {
			dis = zMinMax.y - startPoint.z;
			borderPoint.x = startPoint.x; borderPoint.z = zMinMax.y;
		} 
		Vector3 closestObsPoint = Vector3.zero;
		if (!isFree(startPoint)) {
			//on obstacle
			closestObsPoint = getClosestPoint(ref f, startPoint);
		} else {
			//free area
			//Can opt this call..
			closestObsPoint = getClosestPoint(ref f, getClosestPoint(ref b, startPoint));
		}
		if ((closestObsPoint - startPoint).magnitude < dis) {
			borderPoint = closestObsPoint;
		}
		if (!isFree (borderPoint)) {
			if (borderPoint.x == xMinMax.x || borderPoint.x == xMinMax.y || borderPoint.z == zMinMax.x || borderPoint.z == zMinMax.y) {
			//	Debug.Log ("Corrected point");
				borderPoint = closestObsPoint;
			}
		}
		return borderPoint;
	}

	public map generateRoadMap(int nodes, Vector2 xMinMax, Vector2 zMinMax, bool visibleMap) {
	//	bool[,] notFree = new bool[(int)(xMinMax.y - xMinMax.x), (int)(zMinMax.y - zMinMax.x)];
		sweepMap(xMinMax, zMinMax);
		List<Vector3> map = new List<Vector3> ();
		map m = new map ();
		m.allNodes = new List<CustomNode> ();
		m.spawns = new List<spawnNode> (); 
		m.goals = new List<int> ();
		GameObject graph = new GameObject (); //Empty stub
		foreach(CustomNode c in Object.FindObjectsOfType<CustomNode> ()) {
			map.Add (c.transform.position);
			m.allNodes.Add (c); //Assume a circle threshold
			if (c.gameObject.GetComponent<CustomNode> ().isSpawn) {
				spawnNode sn = new spawnNode ();
				sn.node = map.Count - 1;
				sn.spawner = c.gameObject.transform.parent.gameObject.GetComponent<Spawner>();
				sn.spawner.SetNode(sn.node);
				m.spawns.Add (sn);
				//	c.gameObject.transform.parent.gameObject.GetComponent<Renderer> ().enabled = false;
			} 
			if (c.gameObject.GetComponent<CustomNode> ().isGoal) {
				m.goals.Add (map.Count-1);
			} 
			// Inform the waiting area of its node index in the map
			if(c is WaitingAreaNode)
			{
				c.GetComponent<WaitingArea>().setMapIndex(map.Count - 1);
			}

			Renderer r = c.GetComponent<Renderer> ();
			if (r != null) {
				if (!visibleMap) {
					r.enabled = false;
					for(int k = 0; k < c.transform.childCount; ++k) {
						Renderer child = c.transform.GetChild (k).GetComponent<Renderer> ();
						if (child != null)
							child.enabled = false;
					}
				}
			}
			//c.transform.parent = graph.transform;
		}

		/**
		 * Generate random nodes in the map
		 * Check if they are free
		 * If not free, find the closest free point
		 * If free, add to the list
		 * */
		for(int i = 0; i < nodes; ++i) {
			Vector3 p = new Vector3 (Random.Range (xMinMax.x, xMinMax.y), 0.0f, Random.Range (zMinMax.x, zMinMax.y));
			Vector3 s = p;
			bool free = isFree (p);
			Vector3 q = getBorderDistance(p, xMinMax, zMinMax);
			Vector3 dir = p - q; 
			if (!free) {
				dir = q - p;
				s = q; 
			}
			dir.y = 0f; 
			dir = dir.normalized;
			int cnter = 0;
			int lim = Mathf.Max((int)(xMinMax.y -xMinMax.x)+1, (int)(zMinMax.y - zMinMax.x)+1); //Dont wait to wait forever if something funny happened

			while(true) {

				s += dir;
				cnter += 1;
				if (cnter >= lim) {
//					Debug.Log ("Bad Error in MapGen");
//					Debug.Log ("Origin: " + p);
//					Debug.Log ("q: " + q);
					break;
				}

				Vector3 news = getBorderDistance (s, xMinMax, zMinMax);

				if ((q-s).magnitude > (news-s).magnitude && (q-news).magnitude > 5) {
					break;
				}
					
			}
			if (!isFree (s)) {
				i -= 1;
				continue;
			}

			s.y = 0.01f;
			if (s.x < xMinMax.x || s.x > xMinMax.y || s.z < zMinMax.x || s.z > zMinMax.y) {
				i -= 1;
//				Debug.Log ("Way error");
//				Debug.Log ("Origin: " + p);
//				Debug.Log ("q: " + q);
//				Debug.Log ("Free: " + free);
				continue;
			}
	
			map.Add (s);
			GameObject g = GameObject.CreatePrimitive (PrimitiveType.Cylinder);
			g.GetComponent<Collider> ().enabled = false;
			g.transform.localScale = new Vector3 (DEFAULT_THRESHOLD*2, 0.05f, DEFAULT_THRESHOLD*2); 
			g.transform.position = s;
			g.AddComponent<CustomNode> ();
			g.transform.parent = graph.transform;
			m.allNodes.Add (g.GetComponent<CustomNode>());
			Renderer r = g.GetComponent<Renderer> ();
			if (r != null) {
				if (!visibleMap) {
					r.enabled = false;
					for(int k = 0; k < g.transform.childCount; ++k) {
						Renderer child = g.transform.GetChild (k).GetComponent<Renderer> ();
						if (child != null)
							child.enabled = false;
					}
				}
			}
		}

		roadmap = m;
		// Automatically adding nodes finished


		List<List<float>> dist = makeDist (ref map);
		List<List<List<int>>> shortestPaths = getShortestPaths (ref dist, Mathf.Max(xMinMax.y - xMinMax.x, zMinMax.y - zMinMax.x));
		m.shortestPaths = shortestPaths;
		roadmap.shortestPaths = shortestPaths;
		
		return m;
	}

	private List<List<float>> makeDist(ref List<Vector3> map) {
		List<List<float>> dist = new List<List<float>> ();
		for (int i = 0; i < map.Count; ++i) {
			dist.Add (new List<float> ());
		}
		for (int i = 0; i < map.Count; ++i) {
			for (int j = 0; j < map.Count; ++j) {
				if (i != j) {
					if(roadmap.allNodes[i] is WaitingAreaNode)
					{
						// This prevents agents from using waiting areas as nodes in their path
						// except when their goal is the waiting area
						dist[i].Add (float.MaxValue);
					}
					else if (!Physics.Raycast (map [j], map [i] - map [j], (map [i] - map [j]).magnitude)) {
						dist [i].Add ((map [i] - map [j]).magnitude);
					} else {
						dist [i].Add (float.MaxValue);
					}
				} else {
					dist [i].Add(0);
				}
			}
		}
		return dist;
	}

	private List<List<List<int>>> getShortestPaths(ref List<List<float>> dist, float len) {
		List<List<int>> next = new List<List<int>> ();
		List<List<List<int>>> shortestPaths = new List<List<List<int>>>();
		//initialize step
		for (int i = 0; i < dist.Count; ++i) {
			next.Add (new List<int> ());
			shortestPaths.Add (new List<List<int>> ());
			for (int j = 0; j < dist.Count; ++j) {
				shortestPaths[i].Add (new List<int> ());
				if (dist[i][j] > len) {
					next[i].Add (-1);
				} else {
					next[i].Add(i);
				}
			}
		}

		//Main sequence of floyd-warshall
		for (int k = 0; k < dist.Count; ++k) {
			for (int i = 0; i < dist.Count; ++i) {
				for (int j = 0; j < dist.Count; ++j) {
					if(dist[i][k] + dist[k][j] < dist[i][j]) {
						dist[i][j] = dist[i][k] + dist[k][j];
						next[i][j] = next[k][j];
					}
				}
			}
		}

		//path reconstruction
		for (int i = 0; i < shortestPaths.Count; ++i) {
			for (int j = 0; j < shortestPaths.Count; ++j) {
				shortestPaths[i][j] = path (ref next, i, j);
			}

		}

		return shortestPaths;
	}

	private List<int> path(ref List<List<int>> next, int fromNode, int toNode) {
		List<int> p = new List<int> ();
		if (fromNode == toNode) {
			p.Add (fromNode);
		} else if (next [fromNode] [toNode] < 0) {
			//?
		} else {
			p.AddRange(path(ref next, fromNode, next[fromNode][toNode]));
			p.Add(toNode);
		}
		return p;
	}
}
