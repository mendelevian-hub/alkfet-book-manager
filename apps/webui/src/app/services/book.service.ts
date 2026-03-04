import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';



export interface Book {
  id: string;
  title: string;
  author: string;
  year: number;
  status: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export type BookUpsert = Omit<Book, 'id'>;

@Injectable({ providedIn: 'root' })
export class BookService {
  private baseUrl = '/api/books';

  constructor(private http: HttpClient) { }

  getBooks(page: number, pageSize: number): Observable<PagedResult<Book>> {
    return this.http.get<PagedResult<Book>>(`${this.baseUrl}?page=${page}&pageSize=${pageSize}`);
  }

  getBook(id: string): Observable<Book> {
    return this.http.get<Book>(`${this.baseUrl}/${id}`);
  }

  createBook(req: BookUpsert): Observable<Book> {
    return this.http.post<Book>(this.baseUrl, req);
  }

  updateBook(id: string, req: BookUpsert): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, req);
  }

  deleteBook(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
