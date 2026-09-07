---
layout: default
title: Writing
description: Notes from building an EN 16931 validator in .NET — mostly the things that were not written down anywhere obvious, and the mistakes that were mine.
permalink: /blog/
---

<ul class="post-list">
{% for post in site.posts %}
  <li>
    <p class="post-meta">
      <time datetime="{{ post.date | date_to_xmlschema }}">{{ post.date | date: "%-d %B %Y" }}</time>
    </p>
    <h3><a href="{{ post.url | relative_url }}">{{ post.title }}</a></h3>
    <p>{{ post.description }}</p>
  </li>
{% endfor %}
</ul>

Subscribe with [Atom]({{ '/feed.xml' | relative_url }}), or watch
[the repository](https://github.com/yivo-0/Klarfakt).
