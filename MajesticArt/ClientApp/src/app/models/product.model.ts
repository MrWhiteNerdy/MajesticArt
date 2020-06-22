import { Category } from './category.model';

export interface Product {
  id?: number;
  name: string;
  description: string;
  image: string;
  quantity: number;
  price: number;
  categoryId: number;
  category?: Category;
}