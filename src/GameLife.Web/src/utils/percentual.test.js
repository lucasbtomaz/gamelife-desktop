import { describe, expect, test } from 'vitest';
import { formatarPercentual, limitarPercentual } from './percentual.js';

describe('percentuais de apresentação', () => {
  test('formata inteiros sem decimal e frações com no máximo uma casa', () => {
    expect(formatarPercentual(67)).toBe('67%');
    expect(formatarPercentual(66.666)).toBe('66,7%');
  });

  test('limita valores usados em barras entre zero e cem', () => {
    expect(limitarPercentual(150)).toBe(100);
    expect(limitarPercentual(-5)).toBe(0);
  });

  test('trata valor inválido como zero', () => {
    expect(limitarPercentual('inválido')).toBe(0);
    expect(formatarPercentual('inválido')).toBe('0%');
  });
});
