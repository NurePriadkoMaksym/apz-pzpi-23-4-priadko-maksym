import { Article, ArticleEdge } from "../types";

export interface GraphPoint {
  x: number;
  y: number;
}

export interface RenderedArticleGraph {
  edges: ArticleEdge[];
  positions: Map<number, GraphPoint>;
}

export function buildArticleGraph(articles: Article[], articleEdges: ArticleEdge[]): RenderedArticleGraph {
  const centerX = 50;
  const centerY = 50;
  const radiusX = 35;
  const radiusY = 31;
  const positions = new Map<number, GraphPoint>();
  const articleIds = articles.map((article) => article.id);

  articleIds.forEach((id, index) => {
    const angle = articleIds.length <= 1 ? 0 : (Math.PI * 2 * index) / articleIds.length - Math.PI / 2;
    positions.set(id, {
      x: centerX + Math.cos(angle) * radiusX,
      y: centerY + Math.sin(angle) * radiusY
    });
  });

  const visible = new Set(articleIds);
  const unique = new Set<string>();
  const edges: ArticleEdge[] = [];

  articleEdges
    .filter((edge) => visible.has(edge.from) && visible.has(edge.to))
    .forEach(({ from, to }) => {
      const key = [from, to].sort((a, b) => a - b).join("-");
      if (!unique.has(key)) {
        unique.add(key);
        edges.push({ from, to });
      }
    });

  return { edges, positions };
}

export function getEdgePath(from: GraphPoint, to: GraphPoint, index: number, edges: ArticleEdge[]) {
  const dx = to.x - from.x;
  const dy = to.y - from.y;
  const length = Math.hypot(dx, dy);
  const nodeOffset = Math.min(9, Math.max(4, length * 0.28));

  if (length === 0) {
    return `M ${from.x} ${from.y} C ${from.x + 6} ${from.y - 8}, ${to.x + 8} ${to.y + 6}, ${to.x} ${to.y}`;
  }

  const ux = dx / length;
  const uy = dy / length;
  const px = -uy;
  const py = ux;
  const samePairIndex = edges
    .slice(0, index)
    .filter((edge) => edge.from === edges[index].from && edge.to === edges[index].to).length;
  const reversePairCount = edges.filter((edge) => edge.from === edges[index].to && edge.to === edges[index].from).length;
  const pairOffset = reversePairCount > 0 ? 5 : 0;
  const curveOffset = pairOffset + samePairIndex * 4;

  const x1 = from.x + ux * nodeOffset;
  const y1 = from.y + uy * nodeOffset;
  const x2 = to.x - ux * nodeOffset;
  const y2 = to.y - uy * nodeOffset;
  const cx = (x1 + x2) / 2 + px * curveOffset;
  const cy = (y1 + y2) / 2 + py * curveOffset;

  return `M ${x1} ${y1} Q ${cx} ${cy} ${x2} ${y2}`;
}
