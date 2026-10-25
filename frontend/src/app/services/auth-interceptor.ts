import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = sessionStorage.getItem('trustvault_token');

  if (token && (
  req.url.startsWith('/api/') ||
  req.url.startsWith('https://trustvault-g7zg.onrender.com/api/')
)) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(req);
};
