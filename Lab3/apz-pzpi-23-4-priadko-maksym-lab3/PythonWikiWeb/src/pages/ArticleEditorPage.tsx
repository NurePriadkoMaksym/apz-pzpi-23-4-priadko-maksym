import React, { FormEvent, useState } from "react";
import { ApiClient } from "../api";
import { ConnectionBuilderModal, NEW_ARTICLE_NODE_ID } from "../components/ConnectionBuilderModal";
import { Article, ArticleEdge } from "../types";

interface ArticleEditorPageProps {
  api: ApiClient;
  articles: Article[];
  canCreate: boolean;
  edges: ArticleEdge[];
  msg: (key: string) => string;
  reload: () => Promise<void>;
  run: <T>(action: () => Promise<T>, success?: string) => Promise<T | undefined>;
}

export function ArticleEditorPage({ api, articles, canCreate, edges, msg, reload, run }: ArticleEditorPageProps) {
  const [id, setId] = useState("");
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [xpReward, setXpReward] = useState(10);
  const [xpRequired, setXpRequired] = useState(0);
  const [isLocked, setIsLocked] = useState(false);
  const [linkToArticleIds, setLinkToArticleIds] = useState<number[]>([]);
  const [fromId, setFromId] = useState("");
  const [toId, setToId] = useState("");
  const [builderOpen, setBuilderOpen] = useState(false);
  const [createdEdges, setCreatedEdges] = useState<Array<{ from: number; to: number }>>([]);
  const editedArticleId = id ? Number(id) : null;
  const existingEditedEdges =
    editedArticleId === null
      ? []
      : edges.filter((edge) => edge.from === editedArticleId || edge.to === editedArticleId);
  const builderEdges = id
    ? dedupeEdges([...existingEditedEdges, ...createdEdges])
    : linkToArticleIds.map((articleId) => ({ from: NEW_ARTICLE_NODE_ID, to: articleId }));

  function load(article: Article) {
    setId(String(article.id));
    setTitle(article.title);
    setContent(article.content);
    setXpReward(article.xpReward);
    setXpRequired(article.xpRequired);
    setIsLocked(article.isLocked);
    setLinkToArticleIds([]);
    setFromId(String(article.id));
    setCreatedEdges([]);
  }

  async function save(event: FormEvent) {
    event.preventDefault();
    if (id) await run(() => api.updateArticle(Number(id), { title, content, xpReward, xpRequired, isLocked }));
    else await run(() => api.createArticle({ title, content, xpReward, xpRequired, linkToArticleIds }));
    await reload();
  }

  async function connectArticles(from: number, targetId: number) {
    if (!id) {
      if (from === NEW_ARTICLE_NODE_ID) {
        setLinkToArticleIds((current) => (current.includes(targetId) ? current : [...current, targetId]));
      }
      return;
    }

    await run(() => api.link(from, targetId), msg("linkCreated"));
    setCreatedEdges((current) =>
      current.some((edge) => edge.from === from && edge.to === targetId) ? current : [...current, { from, to: targetId }]
    );
    await reload();
  }

  return (
    <div className="content-grid editor-grid">
      <section className="panel">
        <div className="panel-header">
          <div>
            <p className="eyebrow">{msg("editorWorkspace")}</p>
            <h3>{msg("articles")}</h3>
          </div>
          {canCreate && (
            <button
              className="secondary-button"
              onClick={() => {
                setId("");
                setTitle("");
                setContent("");
                setXpReward(10);
                setXpRequired(0);
                setIsLocked(false);
                setLinkToArticleIds([]);
              }}
            >
              {msg("create")}
            </button>
          )}
        </div>
        <div className="article-list">
          {articles.map((article) => (
            <button key={article.id} className={`article-row ${id === String(article.id) ? "selected" : ""}`} onClick={() => load(article)}>
              <span>{article.title}</span>
              <small>#{article.id}</small>
            </button>
          ))}
        </div>
      </section>
      <section className="panel">
        <div className="panel-header">
          <div>
            <p className="eyebrow">{id ? `#${id}` : msg("editorWorkspace")}</p>
            <h3>{id ? msg("update") : msg("articles")}</h3>
          </div>
        </div>
        {!id && !canCreate ? (
          <p className="helper-text">{msg("selectArticleToEdit")}</p>
        ) : (
        <form className="form-shell" onSubmit={save}>
          <label className="field">
            <span>{msg("title")}</span>
            <input value={title} onChange={(event) => setTitle(event.target.value)} dir="auto" required />
          </label>
          <label className="field">
            <span>{msg("content")}</span>
            <textarea value={content} onChange={(event) => setContent(event.target.value)} dir="auto" required />
          </label>
          <div className="field-row">
            <label className="field">
              <span>{msg("xpReward")}</span>
              <input type="number" value={xpReward} onChange={(event) => setXpReward(Number(event.target.value))} />
            </label>
            <label className="field">
              <span>{msg("xpRequired")}</span>
              <input type="number" value={xpRequired} onChange={(event) => setXpRequired(Number(event.target.value))} />
            </label>
          </div>
          {id ? (
            <label className="switch-row">
              <input type="checkbox" checked={isLocked} onChange={(event) => setIsLocked(event.target.checked)} />
              <span>{msg("locked")}</span>
            </label>
          ) : (
            <label className="field">
              <span>{msg("linkToArticles")}</span>
              <select
                multiple
                className="article-multiselect"
                value={linkToArticleIds.map(String)}
                onChange={(event) =>
                  setLinkToArticleIds(Array.from(event.currentTarget.selectedOptions, (option) => Number(option.value)))
                }
              >
                {articles.map((article) => (
                  <option key={article.id} value={article.id}>
                    #{article.id} {article.title}
                  </option>
                ))}
              </select>
            </label>
          )}
          <div className="button-row">
            <button>{id ? msg("update") : msg("create")}</button>
            {!id && canCreate && (
              <button type="button" className="secondary-button" onClick={() => setBuilderOpen(true)}>
                {msg("visualBuilder")}
              </button>
            )}
            {id && canCreate && (
              <button
                type="button"
                className="danger-button"
                onClick={async () => {
                  await run(() => api.deleteArticle(Number(id)));
                  setId("");
                  await reload();
                }}
              >
                {msg("delete")}
              </button>
            )}
          </div>
        </form>
        )}
        {id && (
          <form className="link-form" onSubmit={async (event) => {
            event.preventDefault();
            await run(() => api.link(Number(fromId), Number(toId)));
            setCreatedEdges((current) =>
              current.some((edge) => edge.from === Number(fromId) && edge.to === Number(toId))
                ? current
                : [...current, { from: Number(fromId), to: Number(toId) }]
            );
            await reload();
          }}>
            <select value={fromId} onChange={(event) => setFromId(event.target.value)} required>
              <option value="">{msg("fromArticle")}</option>
              {articles.map((article) => (
                <option key={article.id} value={article.id}>
                  #{article.id} {article.title}
                </option>
              ))}
            </select>
            <select value={toId} onChange={(event) => setToId(event.target.value)} required>
              <option value="">{msg("toArticle")}</option>
              {articles.map((article) => (
                <option key={article.id} value={article.id}>
                  #{article.id} {article.title}
                </option>
              ))}
            </select>
            <button>{msg("addLink")}</button>
            <button type="button" className="secondary-button" onClick={() => run(() => api.unlink(Number(fromId), Number(toId)))}>
              {msg("removeLink")}
            </button>
            <button type="button" className="secondary-button" onClick={() => setBuilderOpen(true)}>
              {msg("visualBuilder")}
            </button>
          </form>
        )}
      </section>
      {builderOpen && (
        <ConnectionBuilderModal
          articles={articles}
          editedArticleId={editedArticleId}
          edges={builderEdges}
          title={title}
          msg={msg}
          onClose={() => setBuilderOpen(false)}
          onConnect={connectArticles}
        />
      )}
    </div>
  );
}

function dedupeEdges(edges: Array<{ from: number; to: number }>) {
  const seen = new Set<string>();

  return edges.filter((edge) => {
    const key = `${edge.from}-${edge.to}`;
    if (seen.has(key)) return false;
    seen.add(key);
    return true;
  });
}
