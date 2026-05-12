import { ArticleEdge } from "../types";

export function parseGraphMlEdges(xml: string): ArticleEdge[] {
  const document = new DOMParser().parseFromString(xml, "application/xml");
  const graphMlEdges = Array.from(document.getElementsByTagName("edge"));
  const parsed = graphMlEdges
    .map((edge) => ({
      from: Number(edge.getAttribute("source")),
      to: Number(edge.getAttribute("target"))
    }))
    .filter((edge) => Number.isFinite(edge.from) && Number.isFinite(edge.to));

  if (parsed.length > 0) return parsed;

  return parseDotEdges(xml);
}

function parseDotEdges(dot: string): ArticleEdge[] {
  const edgePattern = /"?(\d+)"?\s*->\s*"?(\d+)"?/g;
  const edges: ArticleEdge[] = [];
  let match: RegExpExecArray | null;

  while ((match = edgePattern.exec(dot)) !== null) {
    edges.push({ from: Number(match[1]), to: Number(match[2]) });
  }

  return edges;
}
