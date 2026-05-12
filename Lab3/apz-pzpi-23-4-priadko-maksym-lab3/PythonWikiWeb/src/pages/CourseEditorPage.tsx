import React, { FormEvent, useState } from "react";
import { ApiClient } from "../api";
import { Article, Course } from "../types";

export function CourseEditorPage({
  api,
  articles,
  courses,
  msg,
  onCourseCreated,
  reload,
  run
}: {
  api: ApiClient;
  articles: Article[];
  courses: Course[];
  msg: (key: string) => string;
  onCourseCreated: (course: Course) => void;
  reload: () => Promise<void>;
  run: <T>(action: () => Promise<T>, success?: string) => Promise<T | undefined>;
}) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [xpRequired, setXpRequired] = useState(0);
  const [articleIds, setArticleIds] = useState<number[]>([]);

  async function save(event: FormEvent) {
    event.preventDefault();
    const created = await run(() => api.createCourse({ title, description, xpRequired, articleIds }), msg("courseCreated"));
    if (!created) return;
    onCourseCreated(created);
    setTitle("");
    setDescription("");
    setXpRequired(0);
    setArticleIds([]);
  }

  return (
    <div className="content-grid">
      <section className="panel">
        <div className="panel-header">
          <div>
            <p className="eyebrow">{msg("creatorWorkspace")}</p>
            <h3>{msg("create")}</h3>
          </div>
        </div>
        <form className="form-shell" onSubmit={save}>
          <label className="field">
            <span>{msg("title")}</span>
            <input value={title} onChange={(event) => setTitle(event.target.value)} dir="auto" required />
          </label>
          <label className="field">
            <span>{msg("description")}</span>
            <textarea value={description} onChange={(event) => setDescription(event.target.value)} dir="auto" />
          </label>
          <label className="field">
            <span>{msg("xpRequired")}</span>
            <input type="number" value={xpRequired} onChange={(event) => setXpRequired(Number(event.target.value))} />
          </label>
          <label className="field">
            <span>{msg("articleIds")}</span>
            <select
              multiple
              className="article-multiselect"
              value={articleIds.map(String)}
              onChange={(event) =>
                setArticleIds(Array.from(event.currentTarget.selectedOptions, (option) => Number(option.value)))
              }
            >
              {articles.map((article) => (
                <option key={article.id} value={article.id}>
                  #{article.id} {article.title}
                </option>
              ))}
            </select>
          </label>
          <button>{msg("create")}</button>
        </form>
        <p className="helper-text">{articles.map((article) => `#${article.id} ${article.title}`).join(" · ")}</p>
      </section>
      <section className="panel">
        <div className="panel-header">
          <div>
            <p className="eyebrow">{msg("data")}</p>
            <h3>{msg("courses")}</h3>
          </div>
        </div>
        <div className="management-list">
          {courses.map((course) => (
            <div className="management-row" key={course.id}>
              <div>
                <strong>{course.title}</strong>
                <span>
                  #{course.id} · {course.xpRequired} XP
                </span>
              </div>
              <button
                className="danger-button"
                onClick={async () => {
                  await run(() => api.deleteCourse(course.id));
                  await reload();
                }}
              >
                {msg("delete")}
              </button>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}
