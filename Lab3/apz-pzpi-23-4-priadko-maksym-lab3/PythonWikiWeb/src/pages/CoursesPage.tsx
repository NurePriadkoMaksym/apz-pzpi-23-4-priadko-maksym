import React from "react";
import { EmptyState } from "../components/EmptyState";
import { StatGrid } from "../components/StatGrid";
import { Article, Course, LocaleCode } from "../types";

export function CoursesPage({
  articles,
  courses,
  locale,
  msg
}: {
  articles: Article[];
  courses: Course[];
  locale: LocaleCode;
  msg: (key: string) => string;
}) {
  const names = new Map(articles.map((article) => [article.id, article.title]));

  return (
    <div className="page-stack">
      <StatGrid
        items={[
          { label: msg("courses"), value: courses.length, tone: "blue" },
          { label: msg("articles"), value: articles.length, tone: "green" }
        ]}
      />
      {courses.length === 0 ? (
        <EmptyState title={msg("courses")} text={msg("noSession")} />
      ) : (
        <div className="course-grid">
          {courses.map((course) => (
            <article className="course-card" key={course.id}>
              <div className="course-card-top">
                <span>#{course.id}</span>
                <strong>{course.xpRequired} XP</strong>
              </div>
              <h3>{course.title}</h3>
              <p>{course.description}</p>
              <ol>
                {course.articleIds
                  .map((id) => names.get(id) ?? `#${id}`)
                  .sort(new Intl.Collator(locale).compare)
                  .map((title) => (
                    <li key={title}>{title}</li>
                  ))}
              </ol>
            </article>
          ))}
        </div>
      )}
    </div>
  );
}
