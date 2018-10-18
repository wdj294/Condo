/**
    A node of a binary tree. Each node represents a rectangular area of the texture
    we surface. Internal nodes store rectangles of used data, whereas leaf nodes track
    rectangles of free space. All the rectangles stored in the tree are disjoint.

    ported from:
    http://clb.demon.fi/projects/rectangle-bin-packing

    For more information, see
    - Rectangle packing: http://www.gamedev.net/community/forums/topic.asp?topic_id=392413
    - Packing lightmaps: http://www.blackpawn.com/texts/lightmaps/default.html


  Juan Sebastian Munoz Arango
  naruse@gmail.com
*/
namespace ProDrawCall {
	using UnityEngine;
	using System.Collections;

	public class Node {
	    public Node left;
	    public Node right;
	    private Rect nodeRect;
	    public Rect NodeRect { get { return nodeRect; } set { nodeRect = value;} }

	    public Node() {
	        left = null;
	        right = null;
	    }
	}
}