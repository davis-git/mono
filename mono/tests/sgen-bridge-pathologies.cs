using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class Bridge {
	public int __test;
	public List<object> Links = new List<object> ();
	public static int fin_count;
	~Bridge () {
		++fin_count;
		Links = null;
	}
}

public class NonBridge
{
	public object Link;
}

class Driver {
	const int OBJ_COUNT = 200 * 1000;
	const int LINK_COUNT = 2;
	const int EXTRAS_COUNT = 0;
	const double survival_rate = 0.6;

	/*
	 * Pathological case for the original old algorithm.  Goes
	 * away when merging is replaced by appending with flag
	 * checking.
	 */
	static void SetupLinks () {
		var list = new List<Bridge> ();
		for (int i = 0; i < OBJ_COUNT; ++i) {
			var bridge = new Bridge ();
			list.Add (bridge);
		}

		var r = new Random (100);
		for (int i = 0; i < OBJ_COUNT; ++i) {
			var n = list [i];
			for (int j = 0; j < LINK_COUNT; ++j)
				n.Links.Add (list [r.Next (OBJ_COUNT)]);
			for (int j = 0; j < EXTRAS_COUNT; ++j)
				n.Links.Add (j);
			if (r.NextDouble () <= survival_rate)
				n.__test = 1;
		}
		Console.WriteLine ("-setup done-");
	}

	const int LIST_LENGTH = 20000;
	const int FAN_OUT = 20000;

	/*
	 * Pathological case for the new algorithm.  Goes away with
	 * the single-node elimination optimization, but will still
	 * persist if modified by using a ladder instead of the single
	 * list.
	 */
	static void SetupLinkedFan ()
	{
		var head = new Bridge ();
		var tail = new NonBridge ();
		head.Links.Add (tail);
		for (int i = 0; i < LIST_LENGTH; ++i)
		{
			var obj = new NonBridge ();
			tail.Link = obj;
			tail = obj;
		}
		var list = new List<Bridge> ();
		tail.Link = list;
		for (int i = 0; i < FAN_OUT; ++i)
			list.Add (new Bridge ());
		Console.WriteLine ("-linked fan done-");
	}

	/*
	 * Pathological case for the improved old algorithm.  Goes
	 * away with copy-on-write DynArrays, but will still persist
	 * if modified by using a ladder instead of the single list.
	 */
	static void SetupInverseFan ()
	{
		var tail = new Bridge ();
		object list = tail;
		for (int i = 0; i < LIST_LENGTH; ++i)
		{
			var obj = new NonBridge ();
			obj.Link = list;
			list = obj;
		}
		var heads = new Bridge [FAN_OUT];
		for (int i = 0; i < FAN_OUT; ++i)
		{
			var obj = new Bridge ();
			obj.Links.Add (list);
			heads [i] = obj;
		}
		Console.WriteLine ("-inverse fan done-");
	}

	/*
	 * Pathological case for the bridge in general.  We generate
	 * 2*FAN_OUT bridge objects here, but the output of the bridge
	 * is a graph with FAN_OUT^2 edges.
	 */
	static void SetupDoubleFan ()
	{
		var heads = new Bridge [FAN_OUT];
		for (int i = 0; i < FAN_OUT; ++i)
			heads [i] = new Bridge ();
		var multiplexer = new Bridge [FAN_OUT];
		for (int i = 0; i < FAN_OUT; ++i)
		{
			heads [i].Links.Add (multiplexer);
			multiplexer [i] = new Bridge ();
		}
		Console.WriteLine ("-double fan done-");
	}

	/*
	 * Not necessarily a pathology, but a special case of where we
	 * generate lots of "dead" SCCs.  A non-bridge object that
	 * can't reach a bridge object can safely be removed from the
	 * graph.  In this special case it's a linked list hanging off
	 * a bridge object.  We can handle this by "forwarding" edges
	 * going to non-bridge nodes that have only a single outgoing
	 * edge.  That collapses the whole list into a single node.
	 * We could remove that node, too, by removing non-bridge
	 * nodes with no outgoing edges.
	 */
	static void SetupDeadList ()
	{
		var head = new Bridge ();
		var tail = new NonBridge ();
		head.Links.Add (tail);
		for (int i = 0; i < LIST_LENGTH; ++i)
		{
			var obj = new NonBridge ();
			tail.Link = obj;
			tail = obj;
		}
	}

	/*
	 * Triggered a bug in the forwarding mechanic.
	 */
	static void SetupSelfLinks ()
	{
		var head = new Bridge ();
		var tail = new NonBridge ();
		head.Links.Add (tail);
		tail.Link = tail;
	}

	static void RunTest (ThreadStart setup)
	{
		var t = new Thread (setup);
		t.Start ();
		t.Join ();

		for (int i = 0; i < 5; ++i) {
			GC.Collect ();
			GC.WaitForPendingFinalizers ();
		}

		Console.WriteLine ("-GCs done- {0}", Bridge.fin_count);
	}

	static int Main ()
	{
		RunTest (SetupLinks);
		RunTest (SetupLinkedFan);
		RunTest (SetupInverseFan);
		RunTest (SetupDoubleFan);
		RunTest (SetupDeadList);
		RunTest (SetupSelfLinks);

		for (int i = 0; i < 0; ++i) {
			GC.Collect ();
			GC.WaitForPendingFinalizers ();
			Console.WriteLine ("-Cleanup GC- {0}", Bridge.fin_count);
		}
		return 0;
	}
}
