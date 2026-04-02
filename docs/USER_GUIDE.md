# docs/USER_GUIDE.md


# User Guide – alkfet-book-manager

## 1) Mi ez az alkalmazás?
Egy egyszerű “Book Manager” rendszer:
- könyvek listázása
- új könyv felvitele
- könyv szerkesztése
- könyv törlése

A rendszer 2 fő UI nézetet tartalmaz:
1) **Books list** (`/books`) – lista + lapozás
2) **Book detail** (`/books/new` és `/books/:id`) – create / edit / delete

## 2) Funkciók

### 2.1 Könyvlista (Books)
- megjeleníti a könyveket táblázatban
- lapozás: Prev/Next (page, pageSize)
- a címre kattintva megnyílik a szerkesztő nézet

### 2.2 Új könyv felvétele
- “New book” / “+ New book”
- mezők:
  - Title
  - Author
  - Year
  - Status (Planned / Reading / Finished)
- Mentés után visszavisz a listára

### 2.3 Könyv szerkesztése
- lista nézetben kattints egy könyvre
- módosíts mezőket
- Save

### 2.4 Könyv törlése
- könyv detail oldalon: Delete
- megerősítést kér
- siker esetén visszavisz a listára

## 3) Elérés

### 3.1 Lokális fejlesztői futtatás
- WebUI tipikusan: `http://localhost:4200`

### 3.2 Lokális Kubernetes deploy után
- WebUI: `http://localhost:30080`
- WebApi (teszt): `http://localhost:30081/api/books?page=1&pageSize=10`

## 4) Megjegyzés
Ha a listában nincs adat, hozz létre egy új könyvet a “New book” oldalon, és utána megjelenik a listában.