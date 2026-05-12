package com.example.pythonwiki.ui.graph

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.rememberTransformableState
import androidx.compose.foundation.gestures.transformable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import com.example.pythonwiki.data.ArticleSummary
import com.example.pythonwiki.data.GraphArticleNode
import com.example.pythonwiki.data.GraphConnection
import com.example.pythonwiki.data.GraphSummary
import kotlin.math.PI
import kotlin.math.ceil
import kotlin.math.cos
import kotlin.math.roundToInt
import kotlin.math.sin
import kotlin.math.sqrt

@Composable
fun KnowledgeMap(
    graph: GraphSummary,
    articles: List<ArticleSummary>,
    onOpenArticle: (ArticleSummary) -> Unit
) {
    val articleMap = remember(articles) { articles.associateBy { it.id } }

    Card(shape = RoundedCornerShape(28.dp)) {
        BoxWithConstraints(
            modifier = Modifier
                .fillMaxWidth()
                .height(420.dp)
                .padding(12.dp)
        ) {
            val density = LocalDensity.current
            val widthPx = with(density) { maxWidth.toPx() }
            val heightPx = with(density) { maxHeight.toPx() }
            val nodeHalfWidthPx = with(density) { 52.dp.toPx() }
            val nodeHalfHeightPx = with(density) { 32.dp.toPx() }
            val edgeColor = MaterialTheme.colorScheme.outline.copy(alpha = 0.45f)
            var scale by remember { mutableStateOf(1f) }
            var pan by remember { mutableStateOf(Offset.Zero) }
            val positions = remember(graph.articles, graph.connections, widthPx, heightPx) {
                buildGraphPositions(
                    nodes = graph.articles,
                    connections = graph.connections,
                    width = widthPx,
                    height = heightPx
                )
            }
            val transformableState = rememberTransformableState { zoomChange, panChange, _ ->
                val newScale = (scale * zoomChange).coerceIn(0.75f, 2.6f)
                scale = newScale

                val rawPan = pan + panChange
                val maxX = kotlin.math.max(0f, ((widthPx * newScale) - widthPx) / 2f + 120f)
                val maxY = kotlin.math.max(0f, ((heightPx * newScale) - heightPx) / 2f + 120f)
                pan = Offset(
                    x = rawPan.x.coerceIn(-maxX, maxX),
                    y = rawPan.y.coerceIn(-maxY, maxY)
                )
            }

            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .clip(RoundedCornerShape(24.dp))
                    .background(MaterialTheme.colorScheme.surface)
                    .transformable(state = transformableState)
            ) {
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .graphicsLayer {
                            scaleX = scale
                            scaleY = scale
                            translationX = pan.x
                            translationY = pan.y
                        }
                        .drawBehind {
                            graph.connections.forEach { edge ->
                                val from = positions[edge.fromId] ?: return@forEach
                                val to = positions[edge.toId] ?: return@forEach
                                drawLine(
                                    color = edgeColor,
                                    start = from,
                                    end = to,
                                    strokeWidth = 3f
                                )
                            }
                        }
                ) {
                    graph.articles.forEach { node ->
                        val offset = positions[node.id] ?: return@forEach
                        GraphNodeBubble(
                            node = node,
                            modifier = Modifier.offset {
                                IntOffset(
                                    x = (offset.x - nodeHalfWidthPx).roundToInt(),
                                    y = (offset.y - nodeHalfHeightPx).roundToInt()
                                )
                            },
                            onClick = { articleMap[node.id]?.let(onOpenArticle) }
                        )
                    }
                }

                Text(
                    text = "Pinch to zoom, drag to move",
                    modifier = Modifier
                        .align(Alignment.BottomEnd)
                        .padding(10.dp)
                        .clip(RoundedCornerShape(50))
                        .background(MaterialTheme.colorScheme.surfaceVariant)
                        .padding(horizontal = 10.dp, vertical = 6.dp),
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }
    }
}

private fun buildGraphPositions(
    nodes: List<GraphArticleNode>,
    connections: List<GraphConnection>,
    width: Float,
    height: Float
): Map<Int, Offset> {
    if (nodes.isEmpty()) return emptyMap()
    if (nodes.size == 1) return mapOf(nodes.first().id to Offset(width / 2f, height / 2f))

    val sorted = nodes.sortedByDescending { it.degree }
    val adjacency = buildMap<Int, MutableSet<Int>> {
        nodes.forEach { put(it.id, mutableSetOf()) }
        connections.forEach { edge ->
            getOrPut(edge.fromId) { mutableSetOf() }.add(edge.toId)
            getOrPut(edge.toId) { mutableSetOf() }.add(edge.fromId)
        }
    }
    val components = mutableListOf<List<GraphArticleNode>>()
    val visited = mutableSetOf<Int>()

    sorted.forEach { start ->
        if (!visited.add(start.id)) return@forEach
        val queue = ArrayDeque<Int>()
        val componentIds = mutableListOf<Int>()
        queue.add(start.id)
        while (queue.isNotEmpty()) {
            val current = queue.removeFirst()
            componentIds += current
            adjacency[current].orEmpty()
                .sortedByDescending { neighborId -> nodes.firstOrNull { it.id == neighborId }?.degree ?: 0 }
                .forEach { neighbor ->
                    if (visited.add(neighbor)) {
                        queue.add(neighbor)
                    }
                }
        }
        components += componentIds.mapNotNull { id -> sorted.firstOrNull { it.id == id } }
    }

    val horizontalPadding = 76f
    val verticalPadding = 64f
    val usableWidth = (width - horizontalPadding * 2).coerceAtLeast(1f)
    val usableHeight = (height - verticalPadding * 2).coerceAtLeast(1f)
    val columnCount = ceil(sqrt(components.size.toFloat())).toInt().coerceAtLeast(1)
    val rowCount = ceil(components.size / columnCount.toFloat()).toInt().coerceAtLeast(1)
    val cellWidth = usableWidth / columnCount
    val cellHeight = usableHeight / rowCount
    val minNodeCenterDistance = 136f
    val nodeClearRadius = 132f
    val positions = mutableMapOf<Int, Offset>()

    components.forEachIndexed { componentIndex, component ->
        if (component.isEmpty()) return@forEachIndexed
        val row = componentIndex / columnCount
        val column = componentIndex % columnCount
        val centerX = horizontalPadding + cellWidth * column + cellWidth / 2f
        val centerY = verticalPadding + cellHeight * row + cellHeight / 2f
        val localRadius = maxOf(nodeClearRadius, minOf(cellWidth, cellHeight) * 0.42f)

        if (component.size == 1) {
            positions[component.first().id] = Offset(centerX, centerY)
            return@forEachIndexed
        }

        if (component.size <= 4) {
            val ringRadius = maxOf(
                localRadius,
                (minNodeCenterDistance / (2f * sin((PI / component.size).toFloat()))).coerceAtLeast(nodeClearRadius)
            )
            component.forEachIndexed { index, node ->
                val angle = ((2 * PI) / component.size) * index - PI / 2
                positions[node.id] = Offset(
                    x = (centerX + cos(angle).toFloat() * ringRadius).coerceIn(horizontalPadding, width - horizontalPadding),
                    y = (centerY + sin(angle).toFloat() * ringRadius).coerceIn(verticalPadding, height - verticalPadding)
                )
            }
            return@forEachIndexed
        }

        val root = component.first()
        positions[root.id] = Offset(centerX, centerY)

        val remaining = component.drop(1)
        remaining.forEachIndexed { index, node ->
            val angle = when (remaining.size) {
                1 -> 0.0
                2 -> if (index == 0) PI * 0.82 else PI * 0.18
                3 -> listOf(PI, PI * 0.18, PI * 1.82)[index]
                else -> ((2 * PI) / remaining.size) * index - PI / 2
            }
            val radius = when {
                remaining.size <= 3 -> localRadius
                index < 6 -> localRadius
                else -> localRadius * 1.35f
            }
            positions[node.id] = Offset(
                x = (centerX + cos(angle).toFloat() * radius).coerceIn(horizontalPadding, width - horizontalPadding),
                y = (centerY + sin(angle).toFloat() * radius).coerceIn(verticalPadding, height - verticalPadding)
            )
        }
    }

    return positions
}

@Composable
private fun GraphNodeBubble(
    node: GraphArticleNode,
    modifier: Modifier = Modifier,
    onClick: () -> Unit
) {
    val containerColor = if (node.isLocked) {
        MaterialTheme.colorScheme.surfaceVariant
    } else {
        MaterialTheme.colorScheme.primary
    }
    val contentColor = if (node.isLocked) {
        MaterialTheme.colorScheme.onSurfaceVariant
    } else {
        MaterialTheme.colorScheme.onPrimary
    }

    Card(
        modifier = modifier
            .width(104.dp)
            .clickable(onClick = onClick),
        shape = RoundedCornerShape(18.dp),
        colors = CardDefaults.cardColors(containerColor = containerColor)
    ) {
        Column(
            modifier = Modifier.padding(horizontal = 10.dp, vertical = 8.dp),
            verticalArrangement = Arrangement.spacedBy(4.dp)
        ) {
            Text(
                text = node.title,
                style = MaterialTheme.typography.labelLarge,
                color = contentColor,
                maxLines = 2,
                overflow = TextOverflow.Ellipsis,
                textAlign = TextAlign.Center,
                modifier = Modifier.fillMaxWidth()
            )
            Text(
                text = if (node.isLocked) "XP ${node.xpRequired}" else "Open",
                style = MaterialTheme.typography.bodyMedium,
                color = contentColor.copy(alpha = 0.82f),
                textAlign = TextAlign.Center,
                modifier = Modifier.fillMaxWidth()
            )
        }
    }
}
