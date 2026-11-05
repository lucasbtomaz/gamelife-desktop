import { render, screen } from '@testing-library/react';
import LibraryProgressSummary from './LibraryProgressSummary.jsx';

test('mostra total e as três categorias da referência', () => {
  render(<LibraryProgressSummary summary={{ total: 89, zerados: 19, dropados: 8, naoTerminados: 62 }} />);

  expect(screen.getByText('89')).toBeInTheDocument();
  expect(screen.getByText('19')).toBeInTheDocument();
  expect(screen.getByText('8')).toBeInTheDocument();
  expect(screen.getByText('62')).toBeInTheDocument();
  expect(screen.getByLabelText('19 jogos zerados de 89')).toHaveStyle({ flexGrow: '19' });
  expect(screen.getByLabelText('8 jogos dropados de 89')).toHaveStyle({ flexGrow: '8' });
  expect(screen.getByLabelText('62 jogos não terminados de 89')).toHaveStyle({ flexGrow: '62' });
});

test('mantém todas as categorias legíveis quando ainda não há registros', () => {
  render(<LibraryProgressSummary summary={{ total: 0, zerados: 0, dropados: 0, naoTerminados: 0 }} />);

  expect(screen.getByText('Total')).toBeInTheDocument();
  expect(screen.getByText('Zerados')).toBeInTheDocument();
  expect(screen.getByText('Dropados')).toBeInTheDocument();
  expect(screen.getByText('Não terminados')).toBeInTheDocument();
  expect(screen.getByLabelText('0 jogos zerados de 0')).toHaveStyle({ flexGrow: '1' });
});
